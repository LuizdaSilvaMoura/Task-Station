import { useRef, useState, useCallback, useEffect } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Upload, Info, X, Paperclip } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
  DialogClose,
} from "@/components/ui/dialog";
import type { Task } from "@/types/task";

const MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB
const ACCEPTED_TYPES = [
  "application/pdf",
  "application/msword",
  "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
  "image/png",
  "image/jpeg",
];

const editTaskSchema = z.object({
  title: z
    .string()
    .min(1, "Título é obrigatório")
    .max(200, "Máximo de 200 caracteres"),
  slaHours: z
    .number({ invalid_type_error: "Informe um número válido" })
    .min(1, "Mínimo de 1 hora")
    .max(720, "Máximo de 720 horas (30 dias)"),
  status: z.enum(["PENDING", "DONE"], {
    required_error: "Status é obrigatório",
  }),
});

type EditTaskFormData = z.infer<typeof editTaskSchema>;

interface EditTaskModalProps {
  task: Task | null;
  open: boolean;
  onClose: () => void;
  onUpdate: (
    id: string,
    title: string,
    slaHours: number,
    status: "PENDING" | "DONE",
    file?: File,
    removeFile?: boolean,
  ) => void;
  isLoading?: boolean;
}

export function EditTaskModal({
  task,
  open,
  onClose,
  onUpdate,
  isLoading,
}: EditTaskModalProps) {
  const [file, setFile] = useState<File | null>(null);
  const [fileError, setFileError] = useState<string | null>(null);
  const [isDragging, setIsDragging] = useState(false);
  const [removeCurrentFile, setRemoveCurrentFile] = useState(false);
  const fileRef = useRef<HTMLInputElement>(null);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<EditTaskFormData>({
    resolver: zodResolver(editTaskSchema),
  });

  // Update form when task changes
  useEffect(() => {
    if (task) {
      reset({
        title: task.title,
        slaHours: task.slaHours,
        status: task.status,
      });
      setRemoveCurrentFile(false);
    }
  }, [task, reset]);

  const validateFile = (f: File): boolean => {
    setFileError(null);
    if (f.size > MAX_FILE_SIZE) {
      setFileError("Arquivo excede o limite de 5MB");
      return false;
    }
    if (!ACCEPTED_TYPES.includes(f.type)) {
      setFileError("Tipo de arquivo não permitido");
      return false;
    }
    return true;
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const selected = e.target.files?.[0];
    if (selected && validateFile(selected)) {
      setFile(selected);
    }
  };

  const handleDrop = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(false);
    const dropped = e.dataTransfer.files[0];
    if (dropped && validateFile(dropped)) {
      setFile(dropped);
    }
  }, []);

  const handleDragOver = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(true);
  }, []);

  const handleDragLeave = useCallback(() => {
    setIsDragging(false);
  }, []);

  const removeFile = () => {
    setFile(null);
    setFileError(null);
    if (fileRef.current) fileRef.current.value = "";
  };

  const handleDownloadCurrentFile = () => {
    if (!task || !task.fileName) return;

    try {
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

  const handleRemoveCurrentFile = () => {
    setRemoveCurrentFile(true);
  };

  const onSubmit = (data: EditTaskFormData) => {
    if (!task) return;
    onUpdate(
      task.id,
      data.title,
      data.slaHours,
      data.status,
      file ?? undefined,
      removeCurrentFile,
    );
    setFile(null);
    setFileError(null);
    setRemoveCurrentFile(false);
    onClose();
  };

  const handleOpenChange = (isOpen: boolean) => {
    if (!isOpen) {
      setFile(null);
      setFileError(null);
      setRemoveCurrentFile(false);
      onClose();
    }
  };

  if (!task) return null;

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="font-heading text-lg">
            Editar Tarefa
          </DialogTitle>
          <DialogDescription>
            Atualize os dados da tarefa abaixo.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-5 py-2">
          {/* Title */}
          <div className="space-y-2">
            <Label htmlFor="edit-task-title">
              Título da Tarefa <span className="text-destructive">*</span>
            </Label>
            <Input
              id="edit-task-title"
              placeholder="Digite o título aqui..."
              autoFocus
              {...register("title")}
            />
            {errors.title && (
              <p className="text-xs text-destructive">{errors.title.message}</p>
            )}
          </div>

          {/* SLA */}
          <div className="space-y-2">
            <Label htmlFor="edit-task-sla">
              SLA (em horas) <span className="text-destructive">*</span>
            </Label>
            <Input
              id="edit-task-sla"
              type="number"
              min={1}
              className="w-28"
              {...register("slaHours", { valueAsNumber: true })}
            />
            {errors.slaHours && (
              <p className="text-xs text-destructive">
                {errors.slaHours.message}
              </p>
            )}
            <p className="flex items-center gap-1 text-xs text-muted-foreground">
              <Info className="h-3 w-3" />
              A data limite será recalculada ao salvar.
            </p>
          </div>

          {/* Status */}
          <div className="space-y-2">
            <Label htmlFor="edit-task-status">
              Status <span className="text-destructive">*</span>
            </Label>
            <select
              id="edit-task-status"
              className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
              {...register("status")}
            >
              <option value="PENDING">PENDENTE</option>
              <option value="DONE">CONCLUÍDO</option>
            </select>
            {errors.status && (
              <p className="text-xs text-destructive">{errors.status.message}</p>
            )}
          </div>

          {/* File Upload */}
          <div className="space-y-2">
            <Label>Atualizar Anexo</Label>
            {task.fileName && !file && !removeCurrentFile && (
              <div className="flex items-center gap-2 rounded-md bg-muted/50 px-3 py-2">
                <Paperclip className="h-3.5 w-3.5 text-muted-foreground" />
                <button
                  type="button"
                  onClick={handleDownloadCurrentFile}
                  className="flex-1 text-left text-xs text-primary hover:underline cursor-pointer"
                >
                  {task.fileName}
                </button>
                <button
                  type="button"
                  onClick={handleRemoveCurrentFile}
                  className="rounded-full p-1 hover:bg-destructive/10 transition-colors"
                  title="Remover arquivo"
                >
                  <X className="h-3.5 w-3.5 text-destructive" />
                </button>
              </div>
            )}
            {removeCurrentFile && !file && (
              <p className="text-xs text-muted-foreground italic">
                O arquivo será removido ao salvar
              </p>
            )}
            <input
              ref={fileRef}
              type="file"
              className="hidden"
              onChange={handleFileChange}
              accept=".pdf,.doc,.docx,.png,.jpg,.jpeg"
            />
            <button
              type="button"
              onClick={() => fileRef.current?.click()}
              onDrop={handleDrop}
              onDragOver={handleDragOver}
              onDragLeave={handleDragLeave}
              className={`flex w-full flex-col items-center gap-2 rounded-xl border-2 border-dashed px-4 py-8 text-muted-foreground transition-colors ${
                isDragging
                  ? "border-primary bg-primary/5"
                  : "border-border bg-accent/40 hover:border-primary/40 hover:bg-accent"
              }`}
            >
              <Upload className="h-8 w-8" />
              {file ? (
                <div className="flex items-center gap-2">
                  <span className="text-sm font-medium text-foreground">
                    {file.name}
                  </span>
                  <button
                    type="button"
                    onClick={(e) => {
                      e.stopPropagation();
                      removeFile();
                    }}
                    className="rounded-full p-0.5 hover:bg-destructive/10"
                  >
                    <X className="h-3.5 w-3.5 text-destructive" />
                  </button>
                </div>
              ) : (
                <>
                  <span className="text-sm">
                    Arraste um arquivo aqui ou clique para buscar
                  </span>
                  <span className="text-xs">(PDF, DOC, PNG, JPG — Max: 5MB)</span>
                </>
              )}
            </button>
            {fileError && (
              <p className="text-xs text-destructive">{fileError}</p>
            )}
          </div>

          <DialogFooter className="gap-2 sm:gap-0">
            <DialogClose asChild>
              <Button type="button" variant="secondary">
                Cancelar
              </Button>
            </DialogClose>
            <Button type="submit" disabled={isLoading}>
              {isLoading ? "Salvando..." : "Salvar Alterações"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
