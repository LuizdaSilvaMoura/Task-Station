import { isAfter, parseISO } from "date-fns";
import type { Task, DisplayStatus } from "@/types/task";

/**
 * Derives the display status for a task.
 * If the task is DONE, it's DONE.
 * If the current time is past the SLA expiration, it's OVERDUE.
 * Otherwise, PENDING.
 */
export function getDisplayStatus(task: Task): DisplayStatus {
  if (task.status === "DONE") return "DONE";

  const now = new Date();
  const expiration = parseISO(task.slaExpirationDate);

  if (isAfter(now, expiration)) return "OVERDUE";

  return "PENDING";
}

/**
 * Checks if a task's SLA has expired and it's not done.
 */
export function isOverdue(task: Task): boolean {
  return getDisplayStatus(task) === "OVERDUE";
}
