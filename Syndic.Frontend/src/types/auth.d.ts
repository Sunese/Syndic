export declare module "@auth/core/types" {
  interface Session {
    user: {
      // accessToken: string;
      // refreshToken: string;
      provider: string;
      error?: string;
    } & DefaultSession['user'];
  }
}
