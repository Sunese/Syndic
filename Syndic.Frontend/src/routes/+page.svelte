<script lang="ts">
  import { getFeedRemote } from "../remote/feed.remote";
  import { createSubRemote } from "../remote/subscription.remote";
  import type { PageProps } from "./$types";
  import Button from "$lib/components/ui/button/button.svelte";
  import { Skeleton } from "$lib/components/ui/skeleton/index.js";
  import SkeletonTextWall from "$lib/components/skeleton-text-wall.svelte";
  import { toast } from "svelte-sonner";
  import type { ChannelDto, FetchChannelResult, ItemDto } from "../client";
  import * as Item from "$lib/components/ui/item/index.js";
  import * as AlertDialog from "$lib/components/ui/alert-dialog/index.js";
  import { Input } from "$lib/components/ui/input";
  import { Spinner } from "$lib/components/ui/spinner";
  import { isHttpError } from "@sveltejs/kit";
  import { Rss } from "@lucide/svelte";
  import DOMPurify from "dompurify";
  let { data }: PageProps = $props();

  let showAddSubDialog: boolean = $state(false);
  let addingSub: boolean = $state(false);

  const getTitle = (res: FetchChannelResult) =>
    res.customTitle ?? res.channelTitle ?? res.channel?.link;

  const addInfoToItem = (
    item: ItemDto,
    channel: ChannelDto,
  ): { item: ItemDto; channel: ChannelDto; publishedTime: Date | null } => {
    return {
      item: item,
      channel: channel,
      publishedTime: item.publishedAt ? new Date(item.publishedAt) : null,
    };
  };

  const handleChannels = (
    res: FetchChannelResult[],
  ): { item: ItemDto; channel: ChannelDto; publishedTime: Date | null }[] => {
    const failures = res.filter((x) => !x.success);
    failures.forEach((x) =>
      toast.error(
        "Could not load channel " + getTitle(x) + "\n" + x.errorMessage,
      ),
    );

    const successes = res.filter((x) => x.success);

    return successes
      .flatMap((x) =>
        x.channel!.items!.map((item) => addInfoToItem(item, x.channel!)),
      )
      .sort(
        (a, b) =>
          (b.publishedTime?.getTime() ?? Infinity) -
          (a.publishedTime?.getTime() ?? Infinity),
      );
  };
</script>

<svelte:boundary>
  <div class="flex flex-col justify-center items-center">
    <div class="w-full">
      {#each handleChannels(await getFeedRemote()) as itemData, i}
        {#if i === 0}
          <div class="pb-4 flex justify-center">
            <Button
              variant="outline"
              class="text-xs"
              onclick={async () => await getFeedRemote().refresh()}
              >Refresh Feed</Button
            >
          </div>
        {/if}
        <ul>
          <li class="pb-4">
            <!-- <a
            href={itemData.item.link}
            target="_blank"
            rel="noopener noreferrer"
          >
            {itemData.item.title}
          </a>
          <div>From: {itemData.channel.title}</div> -->
            <div class="flex w-full max-w-md flex-col gap-6">
              <Item.Root variant="outline" class="">
                <Item.Content
                  class="text-pretty"
                  style="word-break: break-word;"
                >
                  <Item.Title class="hover hover:underline">
                    <a target="_blank" href={itemData.item.link}>
                      {itemData.item.title}
                    </a>
                  </Item.Title>
                  {#if itemData.item.imageUrl}
                    <div>
                      <img
                        src={itemData.item.imageUrl ?? ""}
                        alt="Article media"
                        class="rounded-sm"
                      />
                    </div>
                  {/if}

                  <Item.Description>
                    {@html DOMPurify.sanitize(itemData.item.summary ?? "", {
                      ALLOWED_TAGS: ["a"],
                    })}
                  </Item.Description>
                </Item.Content>
                <Item.Footer>
                  <div class="flex flex-col">
                    <div class="text-xs">
                      {itemData.channel.title}
                    </div>
                    <div class="text-gray-600 text-xs">
                      {itemData.publishedTime?.toLocaleString()}
                    </div>
                  </div>
                </Item.Footer>
              </Item.Root>
            </div>
          </li>
        </ul>
      {:else}
        <div class="flex flex-col items-center gap-4 py-16 text-center">
          <Rss class="h-12 w-12 text-muted-foreground" />
          <h2 class="text-xl font-semibold">No subscriptions yet</h2>
          <p class="text-sm text-muted-foreground max-w-sm">
            Add your first RSS feed to start seeing articles here.
          </p>

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
                        getFeedRemote().refresh();
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
        </div>
      {/each}
    </div>
  </div>

  {#snippet pending()}
    <SkeletonTextWall />
  {/snippet}
</svelte:boundary>
