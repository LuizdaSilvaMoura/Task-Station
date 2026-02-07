import { Bell, ClipboardList, Plus } from "lucide-react";
import { Button } from "@/components/ui/button";

interface TaskHeaderProps {
  onNewTask: () => void;
  overdueCount: number;
}

export function TaskHeader({ onNewTask, overdueCount }: TaskHeaderProps) {
  return (
    <header className="sticky top-0 z-30 border-b bg-card/80 backdrop-blur-md">
      <div className="mx-auto flex max-w-6xl items-center justify-between px-4 py-3 sm:px-6 lg:px-8">
        <div className="flex items-center gap-2.5">
          <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-primary">
            <ClipboardList className="h-5 w-5 text-primary-foreground" />
          </div>
          <h1 className="font-heading text-xl font-bold tracking-tight text-foreground">
            Task Station
          </h1>
        </div>

        <div className="flex items-center gap-2">
          <Button variant="ghost" size="icon" className="relative">
            <Bell className="h-5 w-5" />
            {overdueCount > 0 && (
              <span className="absolute -right-0.5 -top-0.5 flex h-4 w-4 items-center justify-center rounded-full bg-destructive text-[10px] font-bold text-destructive-foreground">
                {overdueCount > 9 ? "9+" : overdueCount}
              </span>
            )}
          </Button>
          <Button onClick={onNewTask} className="gap-1.5">
            <Plus className="h-4 w-4" />
            Nova Tarefa
          </Button>
        </div>
      </div>
    </header>
  );
}
