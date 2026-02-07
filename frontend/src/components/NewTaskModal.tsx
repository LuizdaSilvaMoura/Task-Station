import { useRef, useState, useCallback } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Upload, Info, X } from "lucide-react";
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

const MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB
const ACCEPTED_TYPES = [
  "application/pdf",
  "application/msword",
  "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
  "image/png",
  "image/jpeg",
];

const newTaskSchema = z.object({
  title: z
    .string()
    .min(1, "Título é obrigatório")
    .max(200, "Máximo de 200 caracteres"),
  slaHours: z
    .number({ invalid_type_error: "Informe um número válido" })
    .min(1, "Mínimo de 1 hora")
    .max(720, "Máximo de 720 horas (30 dias)"),
});

type NewTaskFormData = z.infer<typeof newTaskSchema>;

interface NewTaskModalProps {
  open: boolean;
  onClose: () => void;
  onCreate: (title: string, slaHours: number, file?: File) => void;
  isLoading?: boolean;
}

export function NewTaskModal({
  open,
  onClose,
  onCreate,
  isLoading,
}: NewTaskModalProps) {
  const [file, setFile] = useState<File | null>(null);
  const [fileError, setFileError] = useState<string | null>(null);
  const [isDragging, setIsDragging] = useState(false);
  const fileRef = useRef<HTMLInputElement>(null);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<NewTaskFormData>({
    resolver: zodResolver(newTaskSchema),
    defaultValues: { title: "", slaHours: 24 },
  });

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

  const onSubmit = (data: NewTaskFormData) => {
    onCreate(data.title, data.slaHours, file ?? undefined);
    reset();
    setFile(null);
    setFileError(null);
    onClose();
  };

  const handleOpenChange = (isOpen: boolean) => {
    if (!isOpen) {
      reset();
      setFile(null);
      setFileError(null);
      onClose();
    }
  };

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="font-heading text-lg">
            Nova Tarefa
          </DialogTitle>
          <DialogDescription>
            Preencha os dados abaixo para criar uma nova tarefa.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-5 py-2">
          {/* Title */}
          <div className="space-y-2">
            <Label htmlFor="task-title">
              Título da Tarefa <span className="text-destructive">*</span>
            </Label>
            <Input
              id="task-title"
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
            <Label htmlFor="task-sla">
              SLA (em horas) <span className="text-destructive">*</span>
            </Label>
            <Input
              id="task-sla"
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
              O tempo corrido começa a contar ao salvar.
            </p>
          </div>

          {/* File Upload */}
          <div className="space-y-2">
            <Label>Anexo</Label>
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
              {isLoading ? "Criando..." : "Criar Tarefa"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
