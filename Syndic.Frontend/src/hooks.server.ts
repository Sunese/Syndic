import { client } from "./client/client.gen"
import { handle as authJSHandle, signOut } from "./auth"
import { getRequestEvent } from "$app/server";
import { getToken } from "@auth/core/jwt";
import { error, type ServerInit } from "@sveltejs/kit";

export const handle = async ({ event, resolve }) => {
  return authJSHandle({
    event,
    resolve
  })
}

export const init: ServerInit = async () => {
  console.log(`SYNDICAPI_HTTP=${process.env.SYNDICAPI_HTTP}`)
  console.log(`AUTH_SECRET=${process.env.AUTH_SECRET}`)
  console.log(`AUTH_URL=${process.env.AUTH_URL}`)
  console.log(`AUTH_AUTHENTIK_ID=${process.env.AUTH_AUTHENTIK_ID}`)
  console.log(`AUTH_AUTHENTIK_CLIENT_SECRET=${process.env.AUTH_AUTHENTIK_CLIENT_SECRET}`)

  client.setConfig({
    baseUrl: process.env.SYNDICAPI_HTTP,
  });

  client.interceptors.request.use(async (request, options) => {
    console.log('hello from interceptor')
    const requestEvent = getRequestEvent();
    // We are calling auth() solely to trigger JWT callback, that refreshes the token if needed
    // This is kind of a waste and would be nice of the getToken() method also could trigger
    // the callback
    // requestEvent.locals.auth();
    // UPDATE: I think this gives race condition issues with setting cookies..
    const tokens = await getToken({
      req: requestEvent.request,
      secureCookie: true,
      secret: process.env.AUTH_SECRET,
      cookieName: "__Secure-authjs.session-token",
      salt: "__Secure-authjs.session-token",
      raw: false
    })

    const accessToken = tokens?.access_token as string;
    if (!accessToken) throw new Error("No access token found in JWT");
    request.headers.set('Authorization', `Bearer ${accessToken}`);
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