# Syndic - A Simple RSS Reader

## 🚧 ...Under construction... 🚧


### Gotcha's

#### Upstream sent too big header while reading response header from upstream

Make sure your proxy (e.g. NGINX, Traefik) supports large buffers.
Authentik responds with a set-cookie header that includes an access token (about 1.000 characters), which
might exceed the buffer size allowed by your proxy.