<script lang="ts">
  import { Button } from "$lib/components/ui/button/index.js";
  import * as Card from "$lib/components/ui/card/index.js";
  import {
    FieldGroup,
    Field,
    FieldLabel,
    FieldDescription,
    FieldSeparator,
  } from "$lib/components/ui/field/index.js";
  import { Input } from "$lib/components/ui/input/index.js";
  import { cn } from "$lib/utils.js";
  import type { HTMLAttributes } from "svelte/elements";
  import * as Tooltip from "$lib/components/ui/tooltip/index.js";
  import { signIn } from "@auth/sveltekit/client";
  let { class: className, ...restProps }: HTMLAttributes<HTMLDivElement> =
    $props();

  const id = $props.id();
</script>

<div class={cn("flex flex-col gap-6", className)} {...restProps}>
  <Card.Root>
    <Card.Header class="text-center">
      <Card.Title class="text-xl">Log in</Card.Title>
      <Card.Description>Log in with an OAuth provider</Card.Description>
    </Card.Header>
    <Card.Content>
      <form>
        <FieldGroup>
          <Field>
            <Button
              variant="outline"
              type="button"
              onclick={() => signIn("github")}
            >
              <svg
                xmlns="http://www.w3.org/2000/svg"
                viewBox="0 0 24 24"
                class="me-2"
                width="16"
                height="16"
                aria-hidden="true"
              >
                <path
                  fill="currentColor"
                  d="M12 .297a12 12 0 00-3.792 23.39c.6.111.82-.26.82-.577 0-.285-.01-1.04-.016-2.04-3.338.726-4.042-1.61-4.042-1.61-.546-1.387-1.333-1.757-1.333-1.757-1.089-.745.083-.73.083-.73 1.205.085 1.84 1.237 1.84 1.237 1.07 1.834 2.809 1.304 3.495.997.108-.775.418-1.304.76-1.604-2.665-.304-5.467-1.332-5.467-5.93 0-1.31.469-2.382 1.236-3.22-.124-.303-.536-1.523.117-3.176 0 0 1.008-.322 3.3 1.23a11.5 11.5 0 016.003 0c2.29-1.552 3.296-1.23 3.296-1.23.656 1.653.244 2.873.12 3.176.77.838 1.236 1.91 1.236 3.22 0 4.61-2.807 5.624-5.48 5.92.43.372.815 1.102.815 2.222 0 1.605-.015 2.898-.015 3.293 0 .32.216.694.825.576A12 12 0 0012 .297z"
                />
              </svg>
              Login with GitHub
            </Button>
            <Button
              variant="outline"
              type="button"
              onclick={() => signIn("google")}
            >
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
                <path
                  d="M12.48 10.92v3.28h7.84c-.24 1.84-.853 3.187-1.787 4.133-1.147 1.147-2.933 2.4-6.053 2.4-4.827 0-8.6-3.893-8.6-8.72s3.773-8.72 8.6-8.72c2.6 0 4.507 1.027 5.907 2.347l2.307-2.307C18.747 1.44 16.133 0 12.48 0 5.867 0 .307 5.387.307 12s5.56 12 12.173 12c3.573 0 6.267-1.173 8.373-3.36 2.16-2.16 2.84-5.213 2.84-7.667 0-.76-.053-1.467-.173-2.053H12.48z"
                  fill="currentColor"
                />
              </svg>
              Login with Google
            </Button>
          </Field>
          <FieldSeparator class="*:data-[slot=field-separator-content]:bg-card">
            Or continue with
          </FieldSeparator>
          <Tooltip.Root>
            <Tooltip.Trigger>
              <Field class="mb-4">
                <FieldLabel for="email-{id}">Email</FieldLabel>
                <Input
                  disabled
                  id="email-{id}"
                  type="email"
                  placeholder="m@example.com"
                  required
                />
              </Field>
              <Field class="mb-4">
                <div class="flex items-center">
                  <FieldLabel for="password-{id}">Password</FieldLabel>
                  <a
                    href="##"
                    class="ms-auto text-sm underline-offset-4 hover:underline"
                  >
                    Forgot your password?
                  </a>
                </div>
                <Input disabled id="password-{id}" type="password" required />
              </Field>
              <Field>
                <Button disabled type="submit" placeholder="Login">Login</Button
                >
                <FieldDescription class="text-center">
                  Don't have an account? <a href="##">Sign up</a>
                </FieldDescription>
              </Field>
            </Tooltip.Trigger>
            <Tooltip.Content>
              <p>Email/Password combo is not supported for now...</p>
            </Tooltip.Content>
          </Tooltip.Root>
        </FieldGroup>
      </form>
    </Card.Content>
  </Card.Root>
  <FieldDescription class="px-6 text-center">
    By clicking continue, you agree to our <a href="##">Terms of Service</a>
    and <a href="##">Privacy Policy</a>.
  </FieldDescription>
</div>
