import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../../core/network/api_result.dart';
import '../../data/repo/calendar_repo.dart';
import '../../models/leave_balance_summary.dart';
import '../../models/paginated_leave_response.dart';
import 'calendar_state.dart';

class CalendarCubit extends Cubit<CalendarState> {
  final CalendarRepository _repo;

  CalendarCubit(this._repo) : super(CalendarState.initial());

  final int pageSize = 10;

  int currentPage = 1;
  int totalPages = 1;

  List<LeaveRequestModels> allLeaves = [];
  List<LeaveRequestModels> allPendingLeaves = [];

  Future<void> refreshDashboard() async {
    await Future.wait([
      getRemainingLeaves(),
      fetchRecentLeaveRequests(pageNumber: 1),
    ]);
  }

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
    currentPage = 1;
    totalPages = 1;
    emit(CalendarState.initial());
  }
}
