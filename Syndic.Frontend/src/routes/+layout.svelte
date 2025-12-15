<script lang="ts">
  import "./layout.css";
  import "../app.css";
  import favicon from "$lib/assets/favicon.svg";
  import { signIn, signOut } from "@auth/sveltekit/client";
  import { Button } from "$lib/components/ui/button/index.js";

  // light/dark mode watcher
  import { ModeWatcher } from "mode-watcher";
  import DarkModeToggler from "$lib/components/ui/DarkModeToggler.svelte";

  // For toast notifications
  import { Toaster } from "$lib/components/ui/sonner/index.js";
  import Spinner from "$lib/components/ui/spinner/spinner.svelte";
  import { type HttpError } from "@sveltejs/kit";
  import { toast } from "svelte-sonner";

  let { children, data } = $props();

  if (data.session && data.session.user.error) {
    console.error(
      "there was an error with the session:",
      data.session.user.error,
    );
    console.error("signing out...");
    toast.error("there was an error with your session, please sign in again");
    signOut({ callbackUrl: "/" })
      .catch((e) => console.error("error signing out:", e))
      .then(() =>
        console.log("signed out due to session error, redirecting to /"),
      );
  }

  let signInClicked = $state(false);

  const doSignIn = async () => {
    try {
      signInClicked = true;
      await signIn("authentik");
    } catch (e) {
      console.error("error during sign in:", e);
      toast.error("error during sign in, please try again");
    } finally {
      signInClicked = false;
    }
  };
</script>

<svelte:head>
  <link rel="icon" href={favicon} />
</svelte:head>

<svelte:boundary
  onerror={(e) => {
    console.error("Caught error in boundary");
    console.error(e);
  }}
>
  <!-- navbar -->
  <div class="p-5 flex justify-between items-center">
    <a class="text-3xl font-bold" href="/">Syndic 🛜</a>
    <div class="flex justify-center items-center space-x-4">
      <div class="flex items-center justify-center w-28">
        {#if !data.session}
          {#if signInClicked}
            <Spinner class="size.3" />
          {:else}
            <Button onclick={doSignIn}>Sign In</Button>
          {/if}
        {/if}
      </div>
      {#if data.session}
        <a href="/subscriptions" class="text-lg hover:underline font-bold"
          >Subscriptions</a
        >
        <Button onclick={async () => await signOut()}>Sign out</Button>
      {/if}
      <DarkModeToggler />
    </div>
  </div>

  <!-- Dark/light mode watcher -->
  <ModeWatcher />

  <!-- For toast notification -->
  <Toaster />

  {#if $effect.pending()}
    <div class="flex justify-center items-center min-h-screen">
      <Spinner class="size.3.5" />
    </div>
  {/if}

  <div class="px-10 pb-10">
    {#if !data.session}
      <div
        class="flex flex-col justify-center items-center min-h-screen space-y-4"
      >
        <p class="text-lg font-semibold">
          Sign in to access Syndic's features.
        </p>
      </div>
    {:else}
      {@render children()}
    {/if}
  </div>

  {#snippet pending()}
    <!-- div that places contents in the middle of the screen -->
    <div class="flex justify-center items-center min-h-screen">
      <Spinner class="size.3.5" />
    </div>
  {/snippet}

  {#snippet failed(error, reset)}
    {#if (error as HttpError).status === 401 || (error as HttpError).status === 403}
      <div
        class="flex flex-col justify-center items-center min-h-screen space-y-4"
      >
        <p class="text-lg font-semibold">
          You are not allowed to access this page :/
        </p>

        <p>Try signing out</p>
        <Button onclick={async () => await signOut()}>Sign Out</Button>
      </div>
    {:else}
      <div class="flex justify-center items-center min-h-screen">
        <Button onclick={reset}>Oops! Click me to try again</Button>
      </div>
    {/if}
  {/snippet}
</svelte:boundary>
