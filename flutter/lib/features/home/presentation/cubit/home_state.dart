part of 'home_cubit.dart';

/// Base state for home screen
abstract class HomeState extends Equatable {
  const HomeState();

  @override
  List<Object?> get props => [];
}

/// Initial loading state
class HomeInitial extends HomeState {
  const HomeInitial();
}

/// State containing home screen data
class HomeLoaded extends HomeState {
  final List<LeaveBalanceModel> leaveBalance;
  final List<LeaveItem> recentLeaves;
  final bool isLoadingBalance;
  final bool isLoadingLeaves;
  final int unreadCount;
  final bool isLoadingUnread;

  final String? errorMessage;

  const HomeLoaded({
    this.leaveBalance = const [],
    this.recentLeaves = const [],
    this.isLoadingBalance = false,
    this.isLoadingLeaves = false,
    this.unreadCount = 0,
    this.isLoadingUnread = false,

    this.errorMessage,
  });

  HomeLoaded copyWith({
    List<LeaveBalanceModel>? leaveBalance,
    List<LeaveItem>? recentLeaves,
    bool? isLoadingBalance,
    bool? isLoadingLeaves,
    int? unreadCount,
    bool? isLoadingUnread,

    String? errorMessage,
    bool clearErrorMessage = false,
  }) {
    return HomeLoaded(
      leaveBalance: leaveBalance ?? this.leaveBalance,
      recentLeaves: recentLeaves ?? this.recentLeaves,
      isLoadingBalance: isLoadingBalance ?? this.isLoadingBalance,
      isLoadingLeaves: isLoadingLeaves ?? this.isLoadingLeaves,
      unreadCount: unreadCount ?? this.unreadCount,
      isLoadingUnread: isLoadingUnread ?? this.isLoadingUnread,

      errorMessage:
      clearErrorMessage ? null : (errorMessage ?? this.errorMessage),
    );
  }

  @override
  List<Object?> get props => [
    leaveBalance,
    recentLeaves,
    isLoadingBalance,
    isLoadingLeaves,

    // ✅ ADD
    unreadCount,
    isLoadingUnread,

    errorMessage,
  ];
}
