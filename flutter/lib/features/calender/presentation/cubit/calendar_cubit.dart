import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../../core/network/api_result.dart';
import '../../data/repo/calendar_repo.dart';
import '../../models/leave_balance_summary.dart';
import '../../models/paginated_leave_response.dart';
import '../../models/calendar_data_models.dart';
import 'calendar_state.dart';

class CalendarCubit extends Cubit<CalendarState> {
  final CalendarRepository _repo;

  CalendarCubit(this._repo) : super(CalendarState.initial());

  final int pageSize = 10;

  int currentPage = 1;
  int totalPages = 1;

  List<LeaveRequestModels> allLeaves = [];
  List<LeaveRequestModels> allPendingLeaves = [];
  
  // Current calendar data
  CalendarDataResponse? _calendarData;

  Future<void> refreshDashboard() async {
    await Future.wait([
      getRemainingLeaves(),
      fetchRecentLeaveRequests(pageNumber: 1),
    ]);
  }

  /// Load calendar data for a given month
  Future<void> loadCalendarData(DateTime focusedMonth) async {
    emit(CalendarState.loading());

    // Don't pass dates - let backend handle defaults
    final result = await _repo.getCalendarData();

    if (result is Success<CalendarDataResponse>) {
      _calendarData = result.data;
      emit(CalendarState.calendarDataLoaded(
        data: result.data,
        focusedDay: focusedMonth,
        selectedDay: null,
      ));
    } else if (result is Error<CalendarDataResponse>) {
      emit(CalendarState.failure(error: result.failure.message));
    }
  }

  /// Called when month changes in table_calendar
  Future<void> onMonthChanged(DateTime focusedDay) async {
    await loadCalendarData(focusedDay);
  }

  /// Called when a date is selected in the calendar
  void onDateSelected(DateTime selectedDay, DateTime focusedDay) {
    if (state is CalendarDataLoaded) {
      final currentState = state as CalendarDataLoaded;
      emit(currentState.copyWith(
        selectedDay: selectedDay,
        focusedDay: focusedDay,
      ));
    }
  }

  /// Get calendar data (for UI use)
  CalendarDataResponse? get calendarData => _calendarData;

  Future<void> getRemainingLeaves() async {
    emit(CalendarState.loading());

    final result = await _repo.getRemainingLeaves();

    if (result is Success<List<LeaveBalanceSummary>>) {
      emit(CalendarState.remainingSuccess(result.data));
    } else if (result is Error<List<LeaveBalanceSummary>>) {
      emit(CalendarState.failure(error: result.failure.message));
    }
  }

  Future<void> fetchRecentLeaveRequests({int pageNumber = 1}) async {
    emit(CalendarState.loading());

    final result = await _repo.getMyLeaves(
      pageNumber: pageNumber,
      pageSize: pageSize,
    );

    if (result is Success<PaginatedLeaveResponse>) {
      final data = result.data;
      allLeaves = data.items;
      currentPage = data.pageNumber;
      totalPages = data.totalPages;
      emit(CalendarState.leavesSuccess(allLeaves));
    } else if (result is Error<PaginatedLeaveResponse>) {
      emit(CalendarState.failure(error: result.failure.message));
    }
  }

  Future<void> fetchPendingLeaves({String? status, int pageNumber = 1}) async {
    emit(CalendarState.loading());

    final result = await _repo.getPendingLeaves(
      status: status,
      pageNumber: pageNumber,
      pageSize: pageSize,
    );

    if (result is Success<PaginatedLeaveResponse>) {
      final data = result.data;

      if (pageNumber == 1) {
        allPendingLeaves = data.items;
      } else {
        allPendingLeaves.addAll(data.items);
      }

      currentPage = data.pageNumber;
      totalPages = data.totalPages;
      emit(CalendarState.pendingSuccess(allPendingLeaves));
    } else if (result is Error<PaginatedLeaveResponse>) {
      emit(CalendarState.failure(error: result.failure.message));
    }
  }

  void clear() {
    allLeaves = [];
    allPendingLeaves = [];
    _calendarData = null;
    currentPage = 1;
    totalPages = 1;
    emit(CalendarState.initial());
  }
}
