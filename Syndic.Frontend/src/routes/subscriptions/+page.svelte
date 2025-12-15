<script lang="ts">
  import { Spinner } from "$lib/components/ui/spinner";
  import {
    createSubscription,
    getSubscriptions,
    type SubscriptionResponse,
  } from "../../client";
  import {
    createSubRemote,
    deleteSubRemote,
    getSubsRemote,
  } from "../../remote/subscription.remote";
  import SkeletonTextWall from "$lib/components/skeleton-text-wall.svelte";
  import type { PageProps } from "./$types";
  import { Button } from "$lib/components/ui/button";
  import * as AlertDialog from "$lib/components/ui/alert-dialog/index.js";
  import { isHttpError } from "@sveltejs/kit";
  import { Input } from "$lib/components/ui/input";
  import { toast } from "svelte-sonner";
  import { ErrorPageLoop } from "@auth/core/errors";

  let { data }: PageProps = $props();

  let showAddSubDialog: boolean = $state(false);
  let addingSub: boolean = $state(false);

  let showDeleteDialogFor: string | null = $state(null);
  let deletingId: string | null = $state(null);

  const doEditSubscription = async (subscription: SubscriptionResponse) => {
    // Implement edit subscription logic here
    alert(`Editing is not supported right now. Make someone do it :-)`);
  };
  const doDeleteSubscription = async (id: string) => {
    try {
      deletingId = id;
      await deleteSubRemote({ id });
    } catch (error) {
    } finally {
      deletingId = null;
      showDeleteDialogFor = null;
      // https://svelte.dev/docs/kit/remote-functions#query-Refreshing-queries
      getSubsRemote().refresh();
    }
  };
</script>

<svelte:boundary>
  <div class="pb-4">
    <ul>
      {#each await getSubsRemote() as subscription}
        <li>
          <div
            class="p-4 border-b border-gray-200 dark:border-gray-700 flex items-center justify-between"
          >
            <div>
              <h2 class="text-xl font-semibold">
                {subscription.customTitle ??
                  subscription.channelTitle ??
                  subscription.channelUrl}
              </h2>
              <h3 class="text-m text-gray-400">
                {subscription.channelUrl}
              </h3>
              <p class="text-sm text-gray-500">
                Subscribed at: {new Date(
                  subscription.subscribedAt,
                ).toLocaleString()}
              </p>
            </div>

            <div class="flex items-center space-x-4">
              <Button
                variant="secondary"
                onclick={async () => await doEditSubscription(subscription)}
                aria-label="Edit subscription"
              >
                Edit
              </Button>
              <AlertDialog.Root open={showDeleteDialogFor === subscription.id}>
                <Button
                  variant="destructive"
                  onclick={() => (showDeleteDialogFor = subscription.id)}
                >
                  Delete
                </Button>
                {#if showDeleteDialogFor === subscription.id}
                  <AlertDialog.Content
                    onclose={() => (showDeleteDialogFor = null)}
                  >
                    <AlertDialog.Header>
                      <AlertDialog.Title
                        >Unsubscribe to {subscription.customTitle ??
                          subscription.channelTitle ??
                          subscription.channelUrl}?</AlertDialog.Title
                      >
                      <AlertDialog.Description>
                        This action cannot be undone.
                      </AlertDialog.Description>
                    </AlertDialog.Header>
                    <AlertDialog.Footer>
                      <Button
                        variant="default"
                        onclick={() => {
                          showDeleteDialogFor = null;
                        }}
                        aria-label="Cancel"
                      >
                        Cancel
                      </Button>
                      <div class="flex items-center justify-between">
                        {#if deletingId === subscription.id}
                          <Spinner></Spinner>
                        {:else}
                          <Button
                            variant="destructive"
                            onclick={async () =>
                              await doDeleteSubscription(subscription.id)}
                            aria-label="Delete subscription"
                          >
                            Delete
                          </Button>
                        {/if}
                      </div>
                    </AlertDialog.Footer>
                  </AlertDialog.Content>
                {/if}
              </AlertDialog.Root>
            </div>
          </div>
        </li>
      {/each}
    </ul>
  </div>

  <AlertDialog.Root open={showAddSubDialog}>
    <Button variant="default" onclick={() => (showAddSubDialog = true)}>
      Add Subscription
    </Button>
    {#if showAddSubDialog}
      <AlertDialog.Content onclose={() => (showAddSubDialog = false)}>
        <div class="p-4">
          <form
            {...createSubRemote.enhance(async ({ form, data, submit }) => {
              try {
                await createSubRemote.validate({ includeUntouched: true });
                const issues = createSubRemote.fields.allIssues();
                if (issues && issues.length > 0) return;

                addingSub = true;
                await submit();
                form.reset();
                showAddSubDialog = false;
                addingSub = false;
                toast.success("Subscription added!");
              } catch (error) {
                showAddSubDialog = false;
                addingSub = false;
                console.error(error);
                toast.error("Oh no! Something went wrong");
                if (isHttpError(error)) toast.error(error.body.message);
              }
            })}
            oninput={() => createSubRemote.validate()}
          >
            <AlertDialog.Header>
              <AlertDialog.Title>Add Subscription</AlertDialog.Title>
              <AlertDialog.Description>
                <!-- Fill our the form below -->
              </AlertDialog.Description>
              <label class="pb-4">
                <div class="font-bold">URL</div>
                <Input
                  {...createSubRemote.fields.url.as("url")}
                  placeholder="https://example.com/rss"
                />
              </label>

              <label class="pb-4">
                <div class="">(Optional) Custom Name</div>
                <Input
                  {...createSubRemote.fields.customTitle.as("text")}
                  placeholder="My Favourite RSS Feed"
                />
              </label>
            </AlertDialog.Header>
            <AlertDialog.Footer class="pt-4">
              <Button
                variant="secondary"
                onclick={() => {
                  showAddSubDialog = false;
                }}
                aria-label="Cancel"
                type="reset"
              >
                Cancel
              </Button>
              {#if addingSub}
                <Button
                  disabled
                  variant="default"
                  aria-label="Add subscription"
                  type="submit"
                  class="w-36"
                >
                  <Spinner></Spinner>
                </Button>
              {:else}
                <Button
                  variant="default"
                  aria-label="Add subscription"
                  type="submit"
                  class="w-36"
                >
                  Add Subscription
                </Button>
              {/if}
            </AlertDialog.Footer>
          </form>
        </div>
      </AlertDialog.Content>
    {/if}
  </AlertDialog.Root>

  {#snippet pending()}
    <SkeletonTextWall />
  {/snippet}

  {#snippet failed(error, reset)}
    <div class="text-2xl">An error occured!</div>

    <div class="">
      {#if isHttpError(error)}
        {error.body.message}
      {/if}
    </div>

    <Button onclick={reset}>Try again</Button>
  {/snippet}
</svelte:boundary>

<svelte:window
  on:keydown={(event) => {
    if (event.repeat) return;

    if (event.key === "Escape") {
      deletingId = null;
      showDeleteDialogFor = null;
      showAddSubDialog = false;
    }
  }}
/>

<style>
  .fixed-width {
    width: 140px; /* pick a width that fits your text */
  }
</style>
