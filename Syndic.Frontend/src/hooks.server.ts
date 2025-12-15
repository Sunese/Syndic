import { client } from "./client/client.gen"
import { handle as authJSHandle, signOut } from "./auth"
import { getRequestEvent } from "$app/server";
import { getToken } from "@auth/core/jwt";
import { AUTH_SECRET } from "$env/static/private";
import { error, type ServerInit } from "@sveltejs/kit";
import { SYNDICAPI_HTTP } from '$env/static/private';

export const handle = async ({ event, resolve }) => {
  return authJSHandle({
    event,
    resolve
  })
}

export const init: ServerInit = async () => {
  client.setConfig({
    baseUrl: SYNDICAPI_HTTP,
  });

  client.interceptors.request.use(async (request, options) => {
    const requestEvent = getRequestEvent();
    // We are calling auth() solely to trigger JWT callback, that refreshes the token if needed
    // This is kind of a waste and would be nice of the getToken() method also could trigger
    // the callback
    // requestEvent.locals.auth();
    // UPDATE: I think this gives race condition issues with setting cookies..
    const tokens = await getToken({
      req: requestEvent.request,
      secureCookie: true,
      secret: AUTH_SECRET,
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