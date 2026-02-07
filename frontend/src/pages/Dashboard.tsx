import { useState, useMemo } from "react";
import type { Task, FilterType, DisplayStatus } from "@/types/task";
import { getDisplayStatus } from "@/lib/sla";
import { useTasks, useCreateTask } from "@/hooks/useTasks";
import { TaskHeader } from "@/components/TaskHeader";
import { StatCards } from "@/components/StatCards";
import { TaskTable } from "@/components/TaskTable";
import { NewTaskModal } from "@/components/NewTaskModal";

const FILTER_LABELS: Record<Exclude<FilterType, null>, string> = {
  OVERDUE: "Em Atraso",
  PENDING: "Tarefas de Hoje",
  DONE: "Concluídos",
};

export default function Dashboard() {
  const { data: tasks = [], isLoading, isError } = useTasks();
  const createTask = useCreateTask();
  const [filter, setFilter] = useState<FilterType>(null);
  const [modalOpen, setModalOpen] = useState(false);

  // Compute display statuses and counts
  const { tasksByStatus, overdueCount, pendingCount, doneCount } =
    useMemo(() => {
      const statusMap = new Map<string, DisplayStatus>();
      let overdue = 0;
      let pending = 0;
      let done = 0;

      for (const task of tasks) {
        const ds = getDisplayStatus(task);
        statusMap.set(task.id, ds);
        if (ds === "OVERDUE") overdue++;
        else if (ds === "PENDING") pending++;
        else done++;
      }

      return {
        tasksByStatus: statusMap,
        overdueCount: overdue,
        pendingCount: pending,
        doneCount: done,
      };
    }, [tasks]);

  // Apply filter
  const filteredTasks = useMemo(() => {
    if (!filter) return tasks;
    return tasks.filter((t) => tasksByStatus.get(t.id) === filter);
  }, [tasks, filter, tasksByStatus]);

  const handleCreate = (title: string, slaHours: number, file?: File) => {
    createTask.mutate({ title, slaHours, file });
  };

  return (
    <div className="min-h-screen bg-background">
      <TaskHeader
        onNewTask={() => setModalOpen(true)}
        overdueCount={overdueCount}
      />

      <main className="mx-auto max-w-6xl px-4 py-8 sm:px-6 lg:px-8">
        <StatCards
          overdue={overdueCount}
          pending={pendingCount}
          done={doneCount}
          activeFilter={filter}
          onFilter={setFilter}
        />

        {filter && (
          <div className="mt-4 flex items-center gap-2 text-sm text-muted-foreground">
            <span>
              Filtro aplicado:{" "}
              <strong className="text-foreground">
                {FILTER_LABELS[filter]}
              </strong>
            </span>
            <button
              onClick={() => setFilter(null)}
              className="rounded-md bg-accent px-2 py-0.5 text-xs font-medium text-accent-foreground transition-colors hover:bg-accent/80"
            >
              ✕ limpar
            </button>
          </div>
        )}

        {isLoading && (
          <div className="mt-8 flex justify-center">
            <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
          </div>
        )}

        {isError && (
          <div className="mt-8 rounded-xl border border-destructive/20 bg-destructive/5 p-6 text-center">
            <p className="text-sm text-destructive">
              Erro ao carregar tarefas. Verifique sua conexão e tente novamente.
            </p>
          </div>
        )}

        {!isLoading && !isError && <TaskTable tasks={filteredTasks} />}
      </main>

      <NewTaskModal
        open={modalOpen}
        onClose={() => setModalOpen(false)}
        onCreate={handleCreate}
        isLoading={createTask.isPending}
      />
    </div>
  );
}
