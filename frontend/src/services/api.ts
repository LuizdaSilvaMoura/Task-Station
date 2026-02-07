import axios from "axios";
import type { Task, CreateTaskPayload, UpdateTaskPayload } from "@/types/task";

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? "https://localhost:60168/api",
  headers: { "Content-Type": "application/json" },
});

export async function fetchTasks(): Promise<Task[]> {
  const { data } = await api.get<Task[]>("/tasks");
  return data;
}

export async function createTask(payload: CreateTaskPayload): Promise<Task> {
  // Always send as multipart/form-data (required by [FromForm] in controller)
  const formData = new FormData();
  formData.append("title", payload.title);
  formData.append("slaHours", String(payload.slaHours));

  if (payload.file) {
    formData.append("file", payload.file);
  }

  const { data } = await api.post<Task>("/tasks", formData, {
    headers: { "Content-Type": "multipart/form-data" },
  });
  return data;
}

export async function updateTask(
  id: string,
  payload: UpdateTaskPayload,
): Promise<Task> {
  // Always send as multipart/form-data (required by [FromForm] in controller)
  const formData = new FormData();
  formData.append("title", payload.title);
  formData.append("slaHours", String(payload.slaHours));
  formData.append("status", payload.status);
  formData.append("removeFile", String(payload.removeFile ?? false));

  if (payload.file) {
    formData.append("file", payload.file);
  }

  const { data } = await api.put<Task>(`/tasks/${id}`, formData, {
    headers: { "Content-Type": "multipart/form-data" },
  });
  return data;
}

export async function updateTaskStatus(
  id: string,
  status: Task["status"],
): Promise<Task> {
  const { data } = await api.patch<Task>(`/tasks/${id}`, { status });
  return data;
}

export default api;
