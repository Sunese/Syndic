import { prerender, query } from '$app/server';
import { error } from '@sveltejs/kit';
import { getFeed } from '../client';
import * as z from "zod";

export const getFeedRemote = query(
  async () => {
    const feed = await getFeed();

    // await new Promise(r => setTimeout(r, 5000));

    // if (feed.error) {
    //   console.error('Error fetching user feed:', feed.error);
    //   throw error(500, 'Failed to fetch user feed');
    // }

    // if (feed.response.status == 403 || feed.response.status == 401) {
    //   throw error(403, 'You are not authorized.');
    // }

    // if (!feed.data) throw error(500, 'Failed to fetch user feed');

    console.log(feed)

    return feed.data?.channels ?? error(500, 'no data')

    // return [{ title: "Test Title", link: "https://example.com", description: "This is a test description.", published: new Date(), items: [{ title: "Test Item", link: "https://example.com/item", description: "This is a test item description.", published: new Date() }] }];
  }
);
