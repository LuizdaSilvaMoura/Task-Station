import { AlertTriangle, CalendarDays, CheckCircle2 } from "lucide-react";
import type { FilterType } from "@/types/task";
import { cn } from "@/lib/utils";

interface StatCardsProps {
  overdue: number;
  pending: number;
  done: number;
  activeFilter: FilterType;
  onFilter: (f: FilterType) => void;
}

export function StatCards({
  overdue,
  pending,
  done,
  activeFilter,
  onFilter,
}: StatCardsProps) {
  const toggle = (f: FilterType) =>
    onFilter(activeFilter === f ? null : f);

  const cards = [
    {
      key: "OVERDUE" as const,
      icon: AlertTriangle,
      value: overdue,
      label: "Em Atraso",
      borderClass: "card-stat-overdue",
      iconColor: "text-destructive",
      ring: "ring-destructive/30",
    },
    {
      key: "PENDING" as const,
      icon: CalendarDays,
      value: pending,
      label: "Tarefas de Hoje",
      borderClass: "card-stat-today",
      iconColor: "text-primary",
      ring: "ring-primary/30",
    },
    {
      key: "DONE" as const,
      icon: CheckCircle2,
      value: done,
      label: "Concluídos",
      borderClass: "card-stat-done",
      iconColor: "text-success",
      ring: "ring-success/30",
    },
  ];

  return (
    <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
      {cards.map((c) => (
        <button
          key={c.key}
          onClick={() => toggle(c.key)}
          className={cn(
            "flex items-center gap-4 rounded-xl bg-card p-5 shadow-sm transition-all hover:shadow-md",
            c.borderClass,
            activeFilter === c.key && `ring-2 ${c.ring} shadow-md`,
          )}
        >
          <div
            className={cn(
              "flex h-11 w-11 shrink-0 items-center justify-center rounded-lg bg-accent",
              c.iconColor,
            )}
          >
            <c.icon className="h-5 w-5" />
          </div>
          <div className="text-left">
            <p className="font-heading text-2xl font-bold text-foreground">
              {c.value}
            </p>
            <p className="text-sm text-muted-foreground">{c.label}</p>
          </div>
        </button>
      ))}
    </div>
  );
}
