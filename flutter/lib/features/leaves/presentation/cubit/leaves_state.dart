part of 'leaves_cubit.dart';

/// Base state for leaves list feature
abstract class LeavesState extends Equatable {
  const LeavesState();

  @override
  List<Object?> get props => [];
}

/// Initial state when screen is first loaded
class LeavesInitial extends LeavesState {
  const LeavesInitial();
}

/// State containing leaves list data
class LeavesLoaded extends LeavesState {
  final List<LeaveStatusModel> statuses;
  final List<LeaveItem> leaves;
  final String? selectedStatus; // null means "All"
  final bool isLoadingStatuses;
  final bool isLoadingLeaves;
  final bool isLoadingMore;
  final int currentPage;
  final bool hasMore;
  final String? errorMessage;

  const LeavesLoaded({
    this.statuses = const [],
    this.leaves = const [],
    this.selectedStatus,
    this.isLoadingStatuses = false,
    this.isLoadingLeaves = false,
    this.isLoadingMore = false,
    this.currentPage = 1,
    this.hasMore = true,
    this.errorMessage,
  });

  /// Filter leaves by selected status (frontend filtering)
  List<LeaveItem> get filteredLeaves {
    if (selectedStatus == null) {
      return leaves;
    }
    return leaves.where((leave) => leave.status == selectedStatus).toList();
  }

  LeavesLoaded copyWith({
    List<LeaveStatusModel>? statuses,
    List<LeaveItem>? leaves,
    String? selectedStatus,
    bool? isLoadingStatuses,
    bool? isLoadingLeaves,
    bool? isLoadingMore,
    int? currentPage,
    bool? hasMore,
    String? errorMessage,
    bool clearSelectedStatus = false,
    bool clearErrorMessage = false,
  }) {
    return LeavesLoaded(
      statuses: statuses ?? this.statuses,
      leaves: leaves ?? this.leaves,
      selectedStatus: clearSelectedStatus ? null : (selectedStatus ?? this.selectedStatus),
      isLoadingStatuses: isLoadingStatuses ?? this.isLoadingStatuses,
      isLoadingLeaves: isLoadingLeaves ?? this.isLoadingLeaves,
      isLoadingMore: isLoadingMore ?? this.isLoadingMore,
      currentPage: currentPage ?? this.currentPage,
      hasMore: hasMore ?? this.hasMore,
      errorMessage: clearErrorMessage ? null : (errorMessage ?? this.errorMessage),
    );
  }

  @override
  List<Object?> get props => [
        statuses,
        leaves,
        selectedStatus,
        isLoadingStatuses,
        isLoadingLeaves,
        isLoadingMore,
        currentPage,
        hasMore,
        errorMessage,
      ];
}
