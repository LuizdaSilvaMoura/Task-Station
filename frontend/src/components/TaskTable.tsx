import { useState } from "react";
import { Paperclip, Pencil } from "lucide-react";
import type { Task } from "@/types/task";
import { getDisplayStatus } from "@/lib/sla";
import { cn } from "@/lib/utils";
import { format, parseISO } from "date-fns";
import { ptBR } from "date-fns/locale";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { EditTaskModal } from "@/components/EditTaskModal";
import { ConfirmStatusModal } from "@/components/ConfirmStatusModal";
import { useUpdateTask, useUpdateTaskStatus } from "@/hooks/useTasks";

interface TaskTableProps {
  tasks: Task[];
}

function StatusBadge({ task }: { task: Task }) {
  const displayStatus = getDisplayStatus(task);

  switch (displayStatus) {
    case "OVERDUE":
      return <Badge variant="destructive">ATRASADO</Badge>;
    case "PENDING":
      return <Badge variant="secondary">PENDENTE</Badge>;
    case "DONE":
      return (
        <Badge className="bg-success text-success-foreground hover:bg-success/90">
          CONCLUÍDO
        </Badge>
      );
  }
}

function formatDate(isoDate: string) {
  return format(parseISO(isoDate), "dd/MM HH:mm", { locale: ptBR });
}

export function TaskTable({ tasks }: TaskTableProps) {
  const [editingTask, setEditingTask] = useState<Task | null>(null);
  const [confirmingTask, setConfirmingTask] = useState<Task | null>(null);
  const { mutate: updateTask, isPending } = useUpdateTask();
  const { mutate: updateTaskStatus, isPending: isUpdatingStatus } = useUpdateTaskStatus();

  const handleEdit = (task: Task) => {
    setEditingTask(task);
  };

  const handleUpdate = (
    id: string,
    title: string,
    slaHours: number,
    status: "PENDING" | "DONE",
    file?: File,
    removeFile?: boolean,
  ) => {
    updateTask(
      { id, payload: { title, slaHours, status, file, removeFile } },
      {
        onSuccess: () => {
          setEditingTask(null);
        },
      },
    );
  };

  const handleCheckboxChange = (task: Task) => {
    // Only allow marking as done for non-completed tasks
    if (task.status === "DONE") return;
    setConfirmingTask(task);
  };

  const handleConfirmStatusChange = () => {
    if (!confirmingTask) return;

    updateTaskStatus(
      { id: confirmingTask.id, status: "DONE" },
      {
        onSuccess: () => {
          setConfirmingTask(null);
        },
      }
    );
  };

  const handleDownloadFile = async (task: Task) => {
    try {
      if (!task.fileName) return;

      // If file data is available as base64 (MongoDB storage), use it directly
      if (task.fileDataBase64 && task.fileContentType) {
        // Convert base64 to blob
        const byteCharacters = atob(task.fileDataBase64);
        const byteNumbers = new Array(byteCharacters.length);
        for (let i = 0; i < byteCharacters.length; i++) {
          byteNumbers[i] = byteCharacters.charCodeAt(i);
        }
        const byteArray = new Uint8Array(byteNumbers);
        const blob = new Blob([byteArray], { type: task.fileContentType });

        // Create blob URL and trigger download
        const blobUrl = window.URL.createObjectURL(blob);
        const link = document.createElement("a");
        link.href = blobUrl;
        link.download = task.fileName;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        window.URL.revokeObjectURL(blobUrl);
      } else if (task.fileUrl) {
        // File is in S3 - direct download
        const link = document.createElement("a");
        link.href = task.fileUrl;
        link.download = task.fileName;
        link.target = "_blank";
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
      }
    } catch (error) {
      console.error("Error downloading file:", error);
      alert("Erro ao baixar o arquivo. Tente novamente.");
    }
  };

  if (!tasks.length) {
    return (
      <div className="mt-8 flex flex-col items-center justify-center rounded-xl border bg-card py-16 text-center">
        <p className="text-lg font-medium text-muted-foreground">
          Nenhuma tarefa encontrada
        </p>
      </div>
    );
  }

  return (
    <>
      <EditTaskModal
        task={editingTask}
        open={!!editingTask}
        onClose={() => setEditingTask(null)}
        onUpdate={handleUpdate}
        isLoading={isPending}
      />
      <ConfirmStatusModal
        task={confirmingTask}
        open={!!confirmingTask}
        onClose={() => setConfirmingTask(null)}
        onConfirm={handleConfirmStatusChange}
        isLoading={isUpdatingStatus}
      />
    <div className="mt-6 overflow-hidden rounded-xl border bg-card shadow-sm">
      <Table>
        <TableHeader>
          <TableRow className="hover:bg-transparent">
            <TableHead className="font-heading font-semibold w-12">
              <span className="sr-only">Concluir</span>
            </TableHead>
            <TableHead className="font-heading font-semibold">
              Título da Tarefa
            </TableHead>
            <TableHead className="font-heading font-semibold">
              Criado em
            </TableHead>
            <TableHead className="font-heading font-semibold">
              SLA Limite
            </TableHead>
            <TableHead className="font-heading font-semibold">Anexo</TableHead>
            <TableHead className="font-heading font-semibold text-right">
              Status
            </TableHead>
            <TableHead className="font-heading font-semibold text-right">
              Ações
            </TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {tasks.map((task) => {
            const displayStatus = getDisplayStatus(task);
            const isOverdue = displayStatus === "OVERDUE";

            return (
              <TableRow
                key={task.id}
                className={cn(
                  "transition-colors",
                  isOverdue && "row-overdue",
                )}
              >
                <TableCell className="w-12">
                  <Checkbox
                    checked={task.status === "DONE"}
                    onCheckedChange={() => handleCheckboxChange(task)}
                    disabled={task.status === "DONE" || isUpdatingStatus}
                    aria-label={`Marcar tarefa "${task.title}" como concluída`}
                  />
                </TableCell>
                <TableCell className="font-medium">
                  {isOverdue && (
                    <span className="mr-1.5 inline-block h-2 w-2 rounded-full bg-destructive" />
                  )}
                  {task.title}
                </TableCell>
                <TableCell className="text-muted-foreground">
                  {formatDate(task.createdAt)}
                </TableCell>
                <TableCell className="text-muted-foreground">
                  {formatDate(task.slaExpirationDate)}
                </TableCell>
                <TableCell>
                  {task.fileName ? (
                    <button
                      type="button"
                      onClick={() => handleDownloadFile(task)}
                      className="inline-flex items-center gap-1 text-primary hover:underline cursor-pointer"
                    >
                      <Paperclip className="h-3.5 w-3.5" />
                      <span className="text-xs font-medium">
                        {task.fileName}
                      </span>
                    </button>
                  ) : (
                    <span className="text-muted-foreground">--</span>
                  )}
                </TableCell>
                <TableCell className="text-right">
                  <StatusBadge task={task} />
                </TableCell>
                <TableCell className="text-right">
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => handleEdit(task)}
                    className="h-8 w-8 p-0"
                  >
                    <Pencil className="h-4 w-4" />
                  </Button>
                </TableCell>
              </TableRow>
            );
          })}
        </TableBody>
      </Table>
    </div>
    </>
  );
}
