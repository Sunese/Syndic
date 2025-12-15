import { SvelteKitAuth, type DefaultSession } from "@auth/sveltekit";
import Authentik from "@auth/core/providers/authentik";
import { AUTH_AUTHENTIK_CLIENT_SECRET, AUTH_AUTHENTIK_ID, AUTH_AUTHENTIK_ISSUER } from "$env/static/private";

export const { handle, signIn, signOut } = SvelteKitAuth({
  providers: [Authentik({
    clientId: AUTH_AUTHENTIK_ID,
    issuer: AUTH_AUTHENTIK_ISSUER,
    clientSecret: AUTH_AUTHENTIK_CLIENT_SECRET,
    authorization: { params: { scope: "openid email profile offline_access" } }
  })],
  trustHost: true,
  session: {
    strategy: "jwt",
  },
  useSecureCookies: true,
  callbacks: {
    async jwt({ token, account }) {
      console.log("JWT CALLBACK TRIGGERED")

      if (account) {
        console.log("Initial login, saving tokens...")
        // Save the access token and refresh token in the JWT on the initial login
        if (!account.expires_in) throw new Error("No expires_in in account")
        return {
          access_token: account.access_token,
          expires_at: Math.floor(Date.now() / 1000 + account.expires_in),
          refresh_token: account.refresh_token,
        }
      }

      const expires_at = token.expires_at as number
      const refresh_token = token.refresh_token as string
      if (!expires_at) throw new Error("No expires_at in token")
      if (!refresh_token) throw new Error("No refresh_token in token")

      if (Date.now() < expires_at * 1000) {
        console.log("Access token is still valid, reusing...")
        // If the access token has not expired yet, return it
        return token
      } else {
        console.log("Access token has expired, refreshing...")
        console.log("refresh token: " + refresh_token)
        // If the access token has expired, try to refresh it
        try {
          const response = await fetch(`https://auth.suneslilleserver.dk/application/o/token/`, {
            headers: { "Content-Type": "application/x-www-form-urlencoded" },
            body: new URLSearchParams({
              client_id: AUTH_AUTHENTIK_ID,
              client_secret: AUTH_AUTHENTIK_CLIENT_SECRET,
              grant_type: "refresh_token",
              refresh_token: refresh_token,
            }),
            method: "POST",
          })

          const tokens = await response.json()

          if (!response.ok) throw tokens

          if (!tokens.access_token) throw new Error("No access_token in refresh response")
          if (!tokens.refresh_token) throw new Error("No refresh_token in refresh response")

          console.log("Successfully refreshed access token")

          return {
            ...token, // Keep the previous token properties
            access_token: tokens.access_token,
            expires_at: Math.floor(Date.now() / 1000 + tokens.expires_in),
            // Fall back to old refresh token, but note that
            // many providers may only allow using a refresh token once.
            refresh_token: tokens.refresh_token,
          }
        } catch (error) {
          console.error("Error refreshing access token", error)
          // The error property will be used client-side to handle the refresh token error
          return { ...token, error: "RefreshAccessTokenError" }
        }
      }
    },

    async session({ session, token }) {
      const error = token.error as string
      session.user.error = error
      return session
    },
  }
})




