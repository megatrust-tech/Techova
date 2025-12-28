"use client";

import { useEffect, useMemo, useState } from "react";

type SelectionMode = "single" | "week" | "range";

type SelectionValue = {
  start: Date | null;
  end: Date | null;
};

type DateRangePickerProps = {
  monthsToShow?: 1 | 2;
  selectionMode?: SelectionMode;
  weekStartsOn?: 0 | 1 | 2 | 3 | 4 | 5 | 6; // 6 = Saturday
  value?: SelectionValue;
  onChange?: (value: SelectionValue) => void;
  minDate?: Date; // Minimum selectable date (dates before this are disabled)
};

type CalendarDay = {
  date: Date;
  inCurrentMonth: boolean;
};

const monthNames = [
  "January",
  "February",
  "March",
  "April",
  "May",
  "June",
  "July",
  "August",
  "September",
  "October",
  "November",
  "December",
];

function startOfWeek(date: Date, weekStartsOn: number) {
  const d = new Date(date);
  const day = d.getDay();
  const diff = (day - weekStartsOn + 7) % 7;
  d.setDate(d.getDate() - diff);
  d.setHours(0, 0, 0, 0);
  return d;
}

function endOfWeek(date: Date, weekStartsOn: number) {
  const s = startOfWeek(date, weekStartsOn);
  const e = new Date(s);
  e.setDate(e.getDate() + 6);
  return e;
}

function addMonths(date: Date, count: number) {
  const d = new Date(date);
  d.setMonth(d.getMonth() + count);
  return d;
}

function isSameDay(a: Date | null, b: Date | null) {
  if (!a || !b) return false;
  return (
    a.getFullYear() === b.getFullYear() &&
    a.getMonth() === b.getMonth() &&
    a.getDate() === b.getDate()
  );
}

function isWithin(date: Date, start: Date | null, end: Date | null) {
  if (!start || !end) return false;
  const time = date.getTime();
  return time >= start.getTime() && time <= end.getTime();
}

function normalizeSelection(
  mode: SelectionMode,
  current: SelectionValue,
  next: Date,
  weekStartsOn: number
): SelectionValue {
  if (mode === "single") {
    return { start: next, end: next };
  }

  if (mode === "week") {
    const start = startOfWeek(next, weekStartsOn);
    const end = endOfWeek(next, weekStartsOn);
    return { start, end };
  }

  // range
  if (!current.start || (current.start && current.end)) {
    return { start: next, end: null };
  }

  const start = current.start;
  if (next.getTime() < start.getTime()) {
    return { start: next, end: start };
  }
  return { start, end: next };
}

function buildCalendar(monthDate: Date, weekStartsOn: number): CalendarDay[] {
  const firstDayOfMonth = new Date(
    monthDate.getFullYear(),
    monthDate.getMonth(),
    1
  );
  const lastDayOfMonth = new Date(
    monthDate.getFullYear(),
    monthDate.getMonth() + 1,
    0
  );

  const start = startOfWeek(firstDayOfMonth, weekStartsOn);
  const end = endOfWeek(lastDayOfMonth, weekStartsOn);

  const days: CalendarDay[] = [];
  const cursor = new Date(start);
  while (cursor <= end) {
    days.push({
      date: new Date(cursor),
      inCurrentMonth: cursor.getMonth() === monthDate.getMonth(),
    });
    cursor.setDate(cursor.getDate() + 1);
  }
  return days;
}

function formatMonth(date: Date) {
  return `${monthNames[date.getMonth()]} ${date.getFullYear()}`;
}

function weekdayLabels(weekStartsOn: number) {
  const labels = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];
  return [...labels.slice(weekStartsOn), ...labels.slice(0, weekStartsOn)];
}

export default function DateRangePicker({
  monthsToShow = 2,
  selectionMode = "range",
  weekStartsOn = 6,
  value,
  onChange,
  minDate,
}: DateRangePickerProps) {
  const [cursorMonth, setCursorMonth] = useState<Date>(() => {
    const now = new Date();
    now.setDate(1);
    now.setHours(0, 0, 0, 0);
    return now;
  });

  const [selection, setSelection] = useState<SelectionValue>(() => {
    if (value) return value;
    return { start: null, end: null };
  });

  useEffect(() => {
    if (value) setSelection(value);
  }, [value]);

  const daysLabel = useMemo(() => weekdayLabels(weekStartsOn), [weekStartsOn]);

  const handleSelect = (day: Date) => {
    // Don't allow selecting dates before minDate
    if (minDate) {
      const minDateNormalized = new Date(minDate);
      minDateNormalized.setHours(0, 0, 0, 0);
      const dayNormalized = new Date(day);
      dayNormalized.setHours(0, 0, 0, 0);
      if (dayNormalized < minDateNormalized) {
        return;
      }
    }

    const nextSelection = normalizeSelection(
      selectionMode,
      selection,
      day,
      weekStartsOn
    );
    setSelection(nextSelection);
    onChange?.(nextSelection);
  };

  const months = useMemo(
    () =>
      Array.from({ length: monthsToShow }, (_, idx) =>
        addMonths(cursorMonth, idx)
      ),
    [cursorMonth, monthsToShow]
  );

  return (
    <div className="datepicker">
      <div className="datepicker__nav">
        <button
          type="button"
          className="nav-btn"
          onClick={() => setCursorMonth(addMonths(cursorMonth, -1))}
          aria-label="Previous month"
        >
          ‹
        </button>
        <div className="nav-title">{formatMonth(cursorMonth)}</div>
        <button
          type="button"
          className="nav-btn"
          onClick={() => setCursorMonth(addMonths(cursorMonth, 1))}
          aria-label="Next month"
        >
          ›
        </button>
      </div>

      <div className={`datepicker__months months-${monthsToShow}`}>
        {months.map((monthDate) => {
          const calendarDays = buildCalendar(monthDate, weekStartsOn);
          return (
            <div className="month" key={monthDate.toISOString()}>
              <div className="month__title">{formatMonth(monthDate)}</div>
              <div className="month__weekdays">
                {daysLabel.map((label) => (
                  <div className="weekday" key={label}>
                    {label}
                  </div>
                ))}
              </div>
              <div className="month__grid">
                {calendarDays.map((day) => {
                  const isSelected =
                    isSameDay(day.date, selection.start) ||
                    isSameDay(day.date, selection.end);
                  const inRange = isWithin(
                    day.date,
                    selection.start,
                    selection.end
                  );

                  // Check if date is before minDate
                  let isDisabled = false;
                  if (minDate) {
                    const minDateNormalized = new Date(minDate);
                    minDateNormalized.setHours(0, 0, 0, 0);
                    const dayNormalized = new Date(day.date);
                    dayNormalized.setHours(0, 0, 0, 0);
                    isDisabled = dayNormalized < minDateNormalized;
                  }

                  return (
                    <button
                      type="button"
                      key={day.date.toISOString()}
                      className={`day ${day.inCurrentMonth ? "" : "day--muted"
                        } ${isSelected ? "day--selected" : ""} ${inRange ? "day--in-range" : ""
                        } ${isDisabled ? "day--disabled" : ""}`}
                      onClick={() => handleSelect(day.date)}
                      disabled={isDisabled}
                      style={isDisabled ? { opacity: 0.3, cursor: "not-allowed" } : undefined}
                    >
                      {day.date.getDate()}
                    </button>
                  );
                })}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}
