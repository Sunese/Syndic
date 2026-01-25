<script lang="ts">
  import { getFeedRemote } from "../remote/feed.remote";
  import type { PageProps } from "./$types";
  import Button from "$lib/components/ui/button/button.svelte";
  import { Skeleton } from "$lib/components/ui/skeleton/index.js";
  import SkeletonTextWall from "$lib/components/skeleton-text-wall.svelte";
  import { toast } from "svelte-sonner";
  import type { ChannelDto, FetchChannelResult, ItemDto } from "../client";
  import * as Item from "$lib/components/ui/item/index.js";
  import DOMPurify from "dompurify";
  let { data }: PageProps = $props();

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
    <div class="pb-4">
      <Button
        variant="outline"
        class="text-xs"
        onclick={async () => await getFeedRemote().refresh()}
        >Refresh Feed</Button
      >
    </div>
    <div>
      {#each handleChannels(await getFeedRemote()) as itemData}
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
      {/each}
    </div>
  </div>

  {#snippet pending()}
    <SkeletonTextWall />
  {/snippet}
</svelte:boundary>
