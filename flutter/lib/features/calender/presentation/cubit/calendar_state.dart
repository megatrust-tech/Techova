import '../../models/leave_balance_summary.dart';
import '../../models/paginated_leave_response.dart';
import '../../models/calendar_data_models.dart';

abstract class CalendarState {
  const CalendarState();

  factory CalendarState.initial() = CalendarInitial;
  factory CalendarState.loading() = CalendarLoading;

  factory CalendarState.remainingSuccess(List<LeaveBalanceSummary> data) =
  CalendarRemainingSuccess;

  factory CalendarState.leavesSuccess(List<LeaveRequestModels> data) =
  CalendarLeavesSuccess;

  factory CalendarState.pendingSuccess(List<LeaveRequestModels> data) =
  CalendarPendingSuccess;

  factory CalendarState.failure({required String error}) = CalendarFailure;

  factory CalendarState.calendarDataLoaded({
    required CalendarDataResponse data,
    required DateTime focusedDay,
    required DateTime? selectedDay,
  }) = CalendarDataLoaded;
}

class CalendarInitial extends CalendarState {
  const CalendarInitial();
}

class CalendarLoading extends CalendarState {
  const CalendarLoading();
}

class CalendarRemainingSuccess extends CalendarState {
  final List<LeaveBalanceSummary> data;
  const CalendarRemainingSuccess(this.data);
}

class CalendarLeavesSuccess extends CalendarState {
  final List<LeaveRequestModels> data;
  const CalendarLeavesSuccess(this.data);
}

class CalendarPendingSuccess extends CalendarState {
  final List<LeaveRequestModels> data;
  const CalendarPendingSuccess(this.data);
}

class CalendarFailure extends CalendarState {
  final String error;
  const CalendarFailure({required this.error});
}

/// State for calendar view with table_calendar
class CalendarDataLoaded extends CalendarState {
  final CalendarDataResponse data;
  final DateTime focusedDay;
  final DateTime? selectedDay;

  const CalendarDataLoaded({
    required this.data,
    required this.focusedDay,
    this.selectedDay,
  });

  CalendarDataLoaded copyWith({
    CalendarDataResponse? data,
    DateTime? focusedDay,
    DateTime? selectedDay,
    bool clearSelectedDay = false,
  }) {
    return CalendarDataLoaded(
      data: data ?? this.data,
      focusedDay: focusedDay ?? this.focusedDay,
      selectedDay: clearSelectedDay ? null : (selectedDay ?? this.selectedDay),
    );
  }
}
