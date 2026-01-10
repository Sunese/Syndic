import { SvelteKitAuth, type DefaultSession } from "@auth/sveltekit";
import Authentik from "@auth/core/providers/authentik";
import GitHub from "@auth/core/providers/github";
import Google from "@auth/core/providers/google";
import Apple from "@auth/core/providers/apple";

export const { handle, signIn, signOut } = SvelteKitAuth({
  providers: [
    GitHub({
      clientId: process.env.AUTH_GITHUB_ID,
      clientSecret: process.env.AUTH_GITHUB_SECRET,
    }),
    Google({
      clientId: process.env.AUTH_GOOGLE_ID,
      clientSecret: process.env.AUTH_GOOGLE_SECRET
    }),
  ],
  trustHost: true,
  secret: process.env.AUTH_SECRET,
  session: {
    strategy: "jwt",
  },
  useSecureCookies: true,
  callbacks: {
    async jwt({ token, account }) {
      if (account?.provider) {
        return { ...token, provider: account.provider }
      }

      return token;
    },

    async session({ session, token }) {
      session.user.provider = token.provider as string;
      return session;
    },
  }
})

// http%3A%2F%2Flocalhost%3A5173%2Fauth%2Fcallback%2Fgithub
// 




