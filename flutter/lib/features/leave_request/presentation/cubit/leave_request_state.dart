part of 'leave_request_cubit.dart';

/// Base state for leave request feature
abstract class LeaveRequestState extends Equatable {
  const LeaveRequestState();

  @override
  List<Object?> get props => [];
}

/// Initial state when screen is first loaded
class LeaveRequestInitial extends LeaveRequestState {
  const LeaveRequestInitial();
}

/// State containing all current form data and status
class LeaveRequestFormState extends LeaveRequestState {
  final List<LeaveTypeModel> leaveTypes;
  final LeaveTypeModel? selectedLeaveType;
  final DateTime? startDate;
  final DateTime? endDate;
  final String? notes;
  final PlatformFile? attachment;
  final bool isLoadingLeaveTypes;
  final bool isCheckingConflict;
  final bool isSubmitting;
  final ConflictCheckResponse? conflictResult;
  final String? errorMessage;
  final bool submitSuccess;

  const LeaveRequestFormState({
    this.leaveTypes = const [],
    this.selectedLeaveType,
    this.startDate,
    this.endDate,
    this.notes,
    this.attachment,
    this.isLoadingLeaveTypes = false,
    this.isCheckingConflict = false,
    this.isSubmitting = false,
    this.conflictResult,
    this.errorMessage,
    this.submitSuccess = false,
  });

  /// Whether the form can be submitted
  bool get canSubmit {
    return selectedLeaveType != null &&
        startDate != null &&
        endDate != null &&
        !isSubmitting &&
        !isCheckingConflict &&
        (conflictResult == null || !conflictResult!.hasConflict);
  }

  /// Calculate number of days for the selected range
  int get numberOfDays {
    if (startDate == null || endDate == null) return 0;
    return endDate!.difference(startDate!).inDays;
  }

  LeaveRequestFormState copyWith({
    List<LeaveTypeModel>? leaveTypes,
    LeaveTypeModel? selectedLeaveType,
    DateTime? startDate,
    DateTime? endDate,
    String? notes,
    PlatformFile? attachment,
    bool? isLoadingLeaveTypes,
    bool? isCheckingConflict,
    bool? isSubmitting,
    ConflictCheckResponse? conflictResult,
    String? errorMessage,
    bool? submitSuccess,
    bool clearSelectedLeaveType = false,
    bool clearStartDate = false,
    bool clearEndDate = false,
    bool clearNotes = false,
    bool clearAttachment = false,
    bool clearConflictResult = false,
    bool clearErrorMessage = false,
  }) {
    return LeaveRequestFormState(
      leaveTypes: leaveTypes ?? this.leaveTypes,
      selectedLeaveType: clearSelectedLeaveType
          ? null
          : (selectedLeaveType ?? this.selectedLeaveType),
      startDate: clearStartDate ? null : (startDate ?? this.startDate),
      endDate: clearEndDate ? null : (endDate ?? this.endDate),
      notes: clearNotes ? null : (notes ?? this.notes),
      attachment: clearAttachment ? null : (attachment ?? this.attachment),
      isLoadingLeaveTypes: isLoadingLeaveTypes ?? this.isLoadingLeaveTypes,
      isCheckingConflict: isCheckingConflict ?? this.isCheckingConflict,
      isSubmitting: isSubmitting ?? this.isSubmitting,
      conflictResult:
          clearConflictResult ? null : (conflictResult ?? this.conflictResult),
      errorMessage:
          clearErrorMessage ? null : (errorMessage ?? this.errorMessage),
      submitSuccess: submitSuccess ?? this.submitSuccess,
    );
  }

  @override
  List<Object?> get props => [
        leaveTypes,
        selectedLeaveType,
        startDate,
        endDate,
        notes,
        attachment,
        isLoadingLeaveTypes,
        isCheckingConflict,
        isSubmitting,
        conflictResult,
        errorMessage,
        submitSuccess,
      ];
}
