"use client";

type LeaveStatus = "pending" | "approved" | "rejected";

type Props = {
  status?: LeaveStatus;
};

export default function LeaveStatusTag({ status = "pending" }: Props) {
  return <span className={`leave-status leave-status-${status}`}>{status}</span>;
}

