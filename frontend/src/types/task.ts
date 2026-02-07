/** Matches the API/MongoDB document shape */
export interface Task {
  id: string;
  title: string;
  createdAt: string; // ISO Date
  slaHours: number;
  slaExpirationDate: string; // ISO Date — calculated on backend
  status: "PENDING" | "DONE";
  fileUrl?: string;
  fileName?: string;
  fileContentType?: string;
  fileDataBase64?: string;
}

/** Payload sent when creating a new task */
export interface CreateTaskPayload {
  title: string;
  slaHours: number;
  file?: File;
}

/** Payload sent when updating a task */
export interface UpdateTaskPayload {
  title: string;
  slaHours: number;
  status: "PENDING" | "DONE";
  file?: File;
  removeFile?: boolean;
}

/** Derived display status (computed on the frontend) */
export type DisplayStatus = "OVERDUE" | "PENDING" | "DONE";

/** Filter applied on the dashboard KPI cards */
export type FilterType = "OVERDUE" | "PENDING" | "DONE" | null;
