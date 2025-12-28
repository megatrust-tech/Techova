part of 'pending_approvals_cubit.dart';

/// Base state for pending approvals feature
abstract class PendingApprovalsState extends Equatable {
  const PendingApprovalsState();

  @override
  List<Object?> get props => [];
}

/// Initial state
class PendingApprovalsInitial extends PendingApprovalsState {
  const PendingApprovalsInitial();
}

/// State containing pending approvals list
class PendingApprovalsLoaded extends PendingApprovalsState {
  final List<LeaveItem> leaves;
  final bool isLoading;
  final bool isLoadingMore;
  final int currentPage;
  final bool hasMore;
  final PendingApprovalCount? pendingCounts;
  final bool isLoadingCount;
  final String? errorMessage;
  final bool isDownloading;
  final String? downloadError;

  const PendingApprovalsLoaded({
    this.leaves = const [],
    this.isLoading = false,
    this.isLoadingMore = false,
    this.currentPage = 1,
    this.hasMore = true,
    this.pendingCounts,
    this.isLoadingCount = false,
    this.errorMessage,
    this.isDownloading = false,
    this.downloadError,
  });

  PendingApprovalsLoaded copyWith({
    List<LeaveItem>? leaves,
    bool? isLoading,
    bool? isLoadingMore,
    int? currentPage,
    bool? hasMore,
    PendingApprovalCount? pendingCounts,
    bool? isLoadingCount,
    String? errorMessage,
    bool clearErrorMessage = false,
    bool? isDownloading,
    String? downloadError,
    bool clearDownloadError = false,
  }) {
    return PendingApprovalsLoaded(
      leaves: leaves ?? this.leaves,
      isLoading: isLoading ?? this.isLoading,
      isLoadingMore: isLoadingMore ?? this.isLoadingMore,
      currentPage: currentPage ?? this.currentPage,
      hasMore: hasMore ?? this.hasMore,
      pendingCounts: pendingCounts ?? this.pendingCounts,
      isLoadingCount: isLoadingCount ?? this.isLoadingCount,
      errorMessage: clearErrorMessage ? null : (errorMessage ?? this.errorMessage),
      isDownloading: isDownloading ?? this.isDownloading,
      downloadError: clearDownloadError ? null : (downloadError ?? this.downloadError),
    );
  }

  @override
  List<Object?> get props => [
        leaves,
        isLoading,
        isLoadingMore,
        currentPage,
        hasMore,
        pendingCounts,
        isLoadingCount,
        errorMessage,
        isDownloading,
        downloadError,
      ];
}
