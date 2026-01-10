import { SignJWT } from 'jose';

const secret = new TextEncoder().encode(
    process.env.INTERNAL_JWT_SECRET!
);

export async function mintInternalJwt(user: {
    name: string;
    email: string;
    provider: string;
}) {
    return await new SignJWT({ provider: user.provider, name: user.name })
        .setProtectedHeader({ alg: 'HS256', kid: 'internal-auth' })
        .setSubject(user.email)
        .setIssuer('internal-auth')
        .setAudience('aspnet-api')
        .setIssuedAt()
        .setExpirationTime('5m')
        .sign(secret);
}
