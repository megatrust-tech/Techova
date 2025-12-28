import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:iconly/iconly.dart';

import '../../../../core/servicelocator/servicelocator.dart';
import '../../../../core/utiles/app_colors.dart';
import '../../data/repositories/leaves_repository.dart';
import '../cubit/pending_approvals_cubit.dart';
import '../widgets/leave_request_card.dart';
import 'leave_review_screen.dart';

/// Screen for pending approvals - used by both Manager and HR roles
class PendingApprovalsScreen extends StatelessWidget {
  /// Status filter: 'PendingManager' for managers, 'PendingHR' for HR
  final String statusFilter;
  
  /// Title to display in app bar
  final String title;

  const PendingApprovalsScreen({
    super.key,
    required this.statusFilter,
    required this.title,
  });

  /// Get reviewer role based on status filter
  String get reviewerRole => statusFilter == 'PendingHR' ? 'hr' : 'manager';

  @override
  Widget build(BuildContext context) {
    return BlocProvider(
      create: (context) => PendingApprovalsCubit(
        sl<LeavesRepository>(),
        statusFilter: statusFilter,
      )..initialize(),
      child: _PendingApprovalsView(title: title, reviewerRole: reviewerRole),
    );
  }
}

class _PendingApprovalsView extends StatelessWidget {
  final String title;
  final String reviewerRole;

  const _PendingApprovalsView({required this.title, required this.reviewerRole});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
        title: Text(title),
        elevation: 0,
        backgroundColor: theme.scaffoldBackgroundColor,
        actions: [
          BlocBuilder<PendingApprovalsCubit, PendingApprovalsState>(
            builder: (context, state) {
              final isDownloading = state is PendingApprovalsLoaded && state.isDownloading;
              return IconButton(
                icon: isDownloading
                    ? const SizedBox(
                        width: 20,
                        height: 20,
                        child: CircularProgressIndicator(strokeWidth: 2),
                      )
                    : Icon(IconlyLight.download),
                tooltip: 'Download Audit Logs',
                onPressed: isDownloading
                    ? null
                    : () => context.read<PendingApprovalsCubit>().downloadAuditLogs(),
              );
            },
          ),
        ],
      ),
      body: BlocConsumer<PendingApprovalsCubit, PendingApprovalsState>(
        listener: (context, state) {
          if (state is PendingApprovalsLoaded && state.downloadError != null) {
            ScaffoldMessenger.of(context).showSnackBar(
              SnackBar(
                content: Text(state.downloadError!),
                backgroundColor: AppColors.error,
                action: state.downloadError == 'No audit logs found'
                    ? null
                    : SnackBarAction(
                        label: 'Retry',
                        textColor: Colors.white,
                        onPressed: () => context.read<PendingApprovalsCubit>().downloadAuditLogs(),
                      ),
              ),
            );
          }
        },
        builder: (context, state) {
          if (state is PendingApprovalsInitial) {
            return const Center(child: CircularProgressIndicator());
          }

          if (state is! PendingApprovalsLoaded) {
            return const Center(child: CircularProgressIndicator());
          }

          return _buildApprovalsList(context, state);
        },
      ),
    );
  }

  Widget _buildApprovalsList(BuildContext context, PendingApprovalsLoaded state) {
    final theme = Theme.of(context);
    final isDark = theme.brightness == Brightness.dark;

    if (state.isLoading) {
      return const Center(child: CircularProgressIndicator());
    }

    if (state.errorMessage != null) {
      return RefreshIndicator(
        onRefresh: () => context.read<PendingApprovalsCubit>().refresh(),
        child: ListView(
          physics: const AlwaysScrollableScrollPhysics(),
          children: [
            SizedBox(height: MediaQuery.of(context).size.height * 0.3),
            Icon(
              Icons.error_outline,
              size: 48,
              color: AppColors.error,
            ),
            const SizedBox(height: 16),
            Center(
              child: Text(
                state.errorMessage!,
                textAlign: TextAlign.center,
                style: TextStyle(color: Colors.grey.shade600),
              ),
            ),
            const SizedBox(height: 16),
            Center(
              child: ElevatedButton(
                onPressed: () => context.read<PendingApprovalsCubit>().refresh(),
                child: const Text('Retry'),
              ),
            ),
          ],
        ),
      );
    }

    if (state.leaves.isEmpty) {
      return RefreshIndicator(
        onRefresh: () => context.read<PendingApprovalsCubit>().refresh(),
        child: ListView(
          physics: const AlwaysScrollableScrollPhysics(),
          children: [
            SizedBox(height: MediaQuery.of(context).size.height * 0.3),
            Icon(
              Icons.check_circle_outline,
              size: 64,
              color: AppColors.success,
            ),
            const SizedBox(height: 16),
            Center(
              child: Text(
                'No pending approvals',
                style: TextStyle(
                  fontSize: 16,
                  color: Colors.grey.shade600,
                ),
              ),
            ),
          ],
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: () => context.read<PendingApprovalsCubit>().refresh(),
      child: NotificationListener<ScrollNotification>(
        onNotification: (notification) {
          if (notification is ScrollEndNotification &&
              notification.metrics.extentAfter < 200 &&
              state.hasMore &&
              !state.isLoadingMore) {
            context.read<PendingApprovalsCubit>().loadMore();
          }
          return false;
        },
        child: ListView.builder(
          padding: const EdgeInsets.all(16),
          // +1 for the header widget
          itemCount: state.leaves.length + 1 + (state.isLoadingMore ? 1 : 0),
          itemBuilder: (context, index) {
            // First item is the pending count header
            if (index == 0) {
              return _buildPendingCountCards(context, state, isDark);
            }

            final leaveIndex = index - 1;

            if (leaveIndex == state.leaves.length) {
              return const Padding(
                padding: EdgeInsets.all(16),
                child: Center(child: CircularProgressIndicator()),
              );
            }

            final leave = state.leaves[leaveIndex];
            // For pending approvals, show employee name from email
            final employeeName = leave.employeeEmail?.split('@').first ?? 'Employee';

            return Padding(
              padding: const EdgeInsets.only(bottom: 12),
              child: LeaveRequestCard(
                leave: leave,
                displayName: employeeName,
                onTap: () async {
                  final result = await Navigator.of(context).push<bool>(
                    MaterialPageRoute(
                      builder: (context) => LeaveReviewScreen(
                        leave: leave,
                        displayName: employeeName,
                        reviewerRole: reviewerRole,
                      ),
                    ),
                  );
                  // Refresh if action was taken
                  if (result == true && context.mounted) {
                    context.read<PendingApprovalsCubit>().refresh();
                  }
                },
              ),
            );
          },
        ),
      ),
    );
  }

  /// Build pending count cards - Manager sees pending manager, HR sees pending HR
  Widget _buildPendingCountCards(BuildContext context, PendingApprovalsLoaded state, bool isDark) {
    final counts = state.pendingCounts;
    final isHR = reviewerRole == 'hr';

    if (state.isLoadingCount || counts == null) {
      return Container(
        margin: const EdgeInsets.only(bottom: 20),
        padding: const EdgeInsets.all(24),
        decoration: BoxDecoration(
          color: isDark ? Colors.grey.shade800 : Colors.grey.shade100,
          borderRadius: BorderRadius.circular(20),
        ),
        child: const Center(
          child: SizedBox(
            width: 24,
            height: 24,
            child: CircularProgressIndicator(strokeWidth: 2),
          ),
        ),
      );
    }

    // HR only sees pending HR approvals
    if (isHR) {
      return _buildSingleCountCard(
        label: 'Pending HR Approvals',
        count: counts.pendingHRApproval,
        color: AppColors.primary,
        icon: Icons.verified_user,
      );
    }

    // Manager only sees pending manager approvals
    return _buildSingleCountCard(
      label: 'Pending Manager Approvals',
      count: counts.pendingManagerApproval,
      color: AppColors.warning,
      icon: Icons.supervisor_account,
    );
  }

  Widget _buildSingleCountCard({
    required String label,
    required int count,
    required Color color,
    required IconData icon,
  }) {
    return Container(
      margin: const EdgeInsets.only(bottom: 20),
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        gradient: LinearGradient(
          colors: [
            color,
            color.withValues(alpha: 0.8),
          ],
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
        ),
        borderRadius: BorderRadius.circular(20),
        boxShadow: [
          BoxShadow(
            color: color.withValues(alpha: 0.3),
            blurRadius: 15,
            offset: const Offset(0, 8),
          ),
        ],
      ),
      child: Row(
        children: [
          Container(
            width: 56,
            height: 56,
            decoration: BoxDecoration(
              color: Colors.white.withValues(alpha: 0.2),
              borderRadius: BorderRadius.circular(16),
            ),
            child: Icon(
              icon,
              color: Colors.white,
              size: 28,
            ),
          ),
          const SizedBox(width: 16),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  label,
                  style: TextStyle(
                    color: Colors.white.withValues(alpha: 0.9),
                    fontSize: 14,
                    fontWeight: FontWeight.w500,
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  '$count',
                  style: const TextStyle(
                    color: Colors.white,
                    fontSize: 32,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
