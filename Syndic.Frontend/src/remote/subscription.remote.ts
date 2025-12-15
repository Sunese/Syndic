import { command, form, prerender, query } from '$app/server';
import { error } from '@sveltejs/kit';
import { createSubscription, deleteSubscription, getFeed, getSubscriptions, type SubscriptionResponse } from '../client';
import * as z from "zod";

export const getSubsRemote = query(
  async () => {
    const subs = await getSubscriptions();

    if (subs.response.status === 403 || subs.response.status === 401) {
      error(403, { message: "You are not authorized" });
    }

    if (subs.error) {
      console.error('Error fetching user subs:', subs.error);
      error(500, { message: 'Failed to fetch user subs' });
    }

    if (!subs.data) error(500, 'Failed to fetch user subs');
    return subs.data;
  }
);

export const createSubRemote = form(
  z.object({
    url: z.url(),
    customTitle: z.optional(z.string()).transform(v => v ?? null).transform(v => v?.length == 0 ? null : v)
  }),
  async ({ url, customTitle }) => {
    const res = await createSubscription({ body: { url, customTitle } })
  }
)

export const deleteSubRemote = command(
  z.object({
    id: z.guid()
  }),
  async ({ id }) => {
    const res = await deleteSubscription({ path: { id: id } })
  }
)
