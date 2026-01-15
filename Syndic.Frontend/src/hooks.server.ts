import { client } from "./client/client.gen"
import { handle as authJSHandle, signOut } from "./auth"
import { getRequestEvent } from "$app/server";
import { getToken } from "@auth/core/jwt";
import { error, redirect, type ServerInit } from "@sveltejs/kit";
import { sequence } from "@sveltejs/kit/hooks";
import { mintInternalJwt } from "$lib/server/jwt";
import { jwtVerify } from "jose";

const redirectIfNotAuthenticated = async ({ event, resolve }: any) => {
  // Protect all routes other than /signin
  if (!event.url.pathname.startsWith('/signin')) {
    const session = await event.locals.auth();
    if (!session) {
      console.log("No session found, redirecting to /signin");
      redirect(303, `/signin`);
    }
  }
  return resolve(event);
}

const authHandle = async ({ event, resolve }: any) => {
  return authJSHandle({
    event,
    resolve
  })
}

export const handle = sequence(authHandle, redirectIfNotAuthenticated);

export const init: ServerInit = async () => {

  client.setConfig({
    baseUrl: process.env.SYNDICAPI_HTTP,
  });

  client.interceptors.request.use(async (request, options) => {
    const requestEvent = getRequestEvent();
    const tokens = await getToken({
      req: requestEvent.request,
      secureCookie: true,
      secret: process.env.AUTH_SECRET,
      cookieName: "__Secure-authjs.session-token",
      salt: "__Secure-authjs.session-token",
      raw: false
    })

    console.log("Tokens in request interceptor:", tokens);
    const name = tokens?.name as string;
    const email = tokens?.email as string;
    const provider = tokens?.provider as string;

    if (!name || !email || !provider) {
      throw error(401, "Missing required token fields");
    }

    const user = {
      name,
      email,
      provider
    }

    const internalJwt = await mintInternalJwt(user);

    request.headers.set('Authorization', `Bearer ${internalJwt}`);

    console.log("Sent request: ", request);
    return request;
  });

  client.interceptors.response.use(async (response) => {
    if (!response.ok) {
      const text = await response.text()
      error(response.status, `Got a non-succesful response (${response.status}): ${text}`)
    }
    return response;
  });
};