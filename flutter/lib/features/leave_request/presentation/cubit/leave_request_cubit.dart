import 'package:dio/dio.dart';
import 'package:equatable/equatable.dart';
import 'package:file_picker/file_picker.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../data/models/conflict_check_model.dart';
import '../../data/models/leave_request_model.dart';
import '../../data/models/leave_type_model.dart';
import '../../data/repositories/leave_repository.dart';

part 'leave_request_state.dart';

/// Cubit for managing leave request form state
class LeaveRequestCubit extends Cubit<LeaveRequestState> {
  final LeaveRepository _repository;

  LeaveRequestCubit(this._repository) : super(const LeaveRequestInitial());

  /// Initialize the form and load leave types
  Future<void> initialize() async {
    emit(const LeaveRequestFormState(isLoadingLeaveTypes: true));

    try {
      final leaveTypes = await _repository.getLeaveTypes();
      emit(LeaveRequestFormState(leaveTypes: leaveTypes));
    } on DioException catch (e) {
      emit(LeaveRequestFormState(
        errorMessage: _extractErrorMessage(e),
      ));
    }
  }

  /// Select a leave type
  void selectLeaveType(LeaveTypeModel leaveType) {
    final currentState = state;
    if (currentState is LeaveRequestFormState) {
      emit(currentState.copyWith(
        selectedLeaveType: leaveType,
        clearErrorMessage: true,
      ));
    }
  }

  /// Set the date range and trigger conflict check
  Future<void> setDateRange(DateTime start, DateTime end) async {
    final currentState = state;
    if (currentState is! LeaveRequestFormState) return;

    emit(currentState.copyWith(
      startDate: start,
      endDate: end,
      isCheckingConflict: true,
      clearConflictResult: true,
      clearErrorMessage: true,
    ));

    try {
      final conflictResult = await _repository.checkConflict(
        startDate: start,
        endDate: end,
      );

      final updatedState = state;
      if (updatedState is LeaveRequestFormState) {
        emit(updatedState.copyWith(
          isCheckingConflict: false,
          conflictResult: conflictResult,
        ));
      }
    } on DioException catch (e) {
      final updatedState = state;
      if (updatedState is LeaveRequestFormState) {
        emit(updatedState.copyWith(
          isCheckingConflict: false,
          errorMessage: _extractErrorMessage(e),
        ));
      }
    }
  }

  /// Update the notes field
  void updateNotes(String notes) {
    final currentState = state;
    if (currentState is LeaveRequestFormState) {
      emit(currentState.copyWith(notes: notes));
    }
  }

  /// Set the attachment file
  void setAttachment(PlatformFile? file) {
    final currentState = state;
    if (currentState is LeaveRequestFormState) {
      if (file != null) {
        emit(currentState.copyWith(attachment: file));
      } else {
        emit(currentState.copyWith(clearAttachment: true));
      }
    }
  }

  /// Remove the current attachment
  void removeAttachment() {
    final currentState = state;
    if (currentState is LeaveRequestFormState) {
      emit(currentState.copyWith(clearAttachment: true));
    }
  }

  /// Submit the leave request
  Future<void> submitLeaveRequest() async {
    final currentState = state;
    if (currentState is! LeaveRequestFormState) return;
    if (!currentState.canSubmit) return;

    emit(currentState.copyWith(isSubmitting: true, clearErrorMessage: true));

    try {
      final request = LeaveRequestModel(
        leaveType: currentState.selectedLeaveType!.value,
        startDate: currentState.startDate!,
        endDate: currentState.endDate!,
        notes: currentState.notes,
      );

      await _repository.submitLeaveRequest(
        request: request,
        attachment: currentState.attachment,
      );

      final updatedState = state;
      if (updatedState is LeaveRequestFormState) {
        emit(updatedState.copyWith(
          isSubmitting: false,
          submitSuccess: true,
        ));
      }
    } on DioException catch (e) {
      final updatedState = state;
      if (updatedState is LeaveRequestFormState) {
        emit(updatedState.copyWith(
          isSubmitting: false,
          errorMessage: _extractErrorMessage(e),
        ));
      }
    }
  }

  /// Reset the form to initial state
  void reset() {
    emit(const LeaveRequestInitial());
    initialize();
  }

  /// Extract user-friendly error message from DioException
  String _extractErrorMessage(DioException e) {
    if (e.response != null) {
      final data = e.response!.data;
      if (data is Map<String, dynamic>) {
        final message = data['message'] ?? data['Message'] ?? data['error'];
        if (message != null) return message.toString();
      }

      switch (e.response!.statusCode) {
        case 400:
          return 'Invalid request. Please check your input.';
        case 401:
          return 'Session expired. Please login again.';
        case 403:
          return 'Access denied.';
        case 404:
          return 'Service not found.';
        case 500:
          return 'Server error. Please try again later.';
        default:
          return 'Error: ${e.response!.statusCode}';
      }
    }

    if (e.type == DioExceptionType.connectionTimeout ||
        e.type == DioExceptionType.receiveTimeout) {
      return 'Connection timeout. Please check your internet.';
    }

    if (e.type == DioExceptionType.connectionError) {
      return 'Unable to connect to server.';
    }

    return 'Network error. Please try again.';
  }
}
