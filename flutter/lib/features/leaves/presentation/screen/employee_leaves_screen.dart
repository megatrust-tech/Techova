import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../../core/servicelocator/servicelocator.dart';
import '../../../../core/utiles/app_colors.dart';
import '../../../../l10n/app_localizations.dart';
import '../../../user/presentation/cubit/user_cubit.dart';
import '../../data/repositories/leaves_repository.dart';
import '../cubit/leaves_cubit.dart';
import '../widgets/leave_request_card.dart';
import '../widgets/status_filter_chips.dart';
import 'leave_details_screen.dart';

/// Employee leaves screen showing their leave requests
class EmployeeLeavesScreen extends StatelessWidget {
  const EmployeeLeavesScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocProvider(
      create: (context) => LeavesCubit(sl<LeavesRepository>())..initialize(),
      child: const _EmployeeLeavesView(),
    );
  }
}

class _EmployeeLeavesView extends StatelessWidget {
  const _EmployeeLeavesView();

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final l10n = AppLocalizations.of(context)!;

    return Scaffold(
      appBar: AppBar(
        title: Text(l10n.myLeaves),
        elevation: 0,
        backgroundColor: theme.scaffoldBackgroundColor,
      ),
      body: BlocBuilder<LeavesCubit, LeavesState>(
        builder: (context, state) {
          if (state is LeavesInitial) {
            return const Center(child: CircularProgressIndicator());
          }

          if (state is! LeavesLoaded) {
            return const Center(child: CircularProgressIndicator());
          }

          return Column(
            children: [
              // Status filter chips
              Padding(
                padding: const EdgeInsets.symmetric(vertical: 12),
                child: StatusFilterChips(
                  statuses: state.statuses,
                  selectedStatus: state.selectedStatus,
                  onSelected: (status) {
                    context.read<LeavesCubit>().selectStatus(status);
                  },
                  isLoading: state.isLoadingStatuses,
                ),
              ),
              // Leaves list
              Expanded(
                child: _buildLeavesList(context, state),
              ),
            ],
          );
        },
      ),
    );
  }

  Widget _buildLeavesList(BuildContext context, LeavesLoaded state) {
    final l10n = AppLocalizations.of(context)!;
    
    // Get the logged-in user's name from UserCubit
    final userState = context.watch<UserCubit>().state;
    String? userName;
    if (userState is UserLoaded) {
      userName = userState.user.fullName;
    }

    if (state.isLoadingLeaves) {
      return const Center(child: CircularProgressIndicator());
    }

    if (state.errorMessage != null) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(
              Icons.error_outline,
              size: 48,
              color: AppColors.error,
            ),
            const SizedBox(height: 16),
            Text(
              state.errorMessage!,
              textAlign: TextAlign.center,
              style: TextStyle(color: Colors.grey.shade600),
            ),
            const SizedBox(height: 16),
            ElevatedButton(
              onPressed: () => context.read<LeavesCubit>().refresh(),
              child: const Text('Retry'),
            ),
          ],
        ),
      );
    }

    final filteredLeaves = state.filteredLeaves;

    if (filteredLeaves.isEmpty) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(
              Icons.event_busy,
              size: 64,
              color: Colors.grey.shade400,
            ),
            const SizedBox(height: 16),
            Text(
              state.selectedStatus != null
                  ? 'No ${state.selectedStatus} leaves'
                  : l10n.comingSoon.replaceAll('Coming Soon...', 'No leaves found'),
              style: TextStyle(
                fontSize: 16,
                color: Colors.grey.shade600,
              ),
            ),
          ],
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: () => context.read<LeavesCubit>().refresh(),
      child: NotificationListener<ScrollNotification>(
        onNotification: (notification) {
          if (notification is ScrollEndNotification &&
              notification.metrics.extentAfter < 200 &&
              state.hasMore &&
              !state.isLoadingMore) {
            context.read<LeavesCubit>().loadMore();
          }
          return false;
        },
        child: ListView.builder(
          padding: const EdgeInsets.symmetric(horizontal: 16),
          itemCount: filteredLeaves.length + (state.isLoadingMore ? 1 : 0),
          itemBuilder: (context, index) {
            if (index == filteredLeaves.length) {
              return const Padding(
                padding: EdgeInsets.all(16),
                child: Center(child: CircularProgressIndicator()),
              );
            }

            return Padding(
              padding: const EdgeInsets.only(bottom: 12),
              child: LeaveRequestCard(
                leave: filteredLeaves[index],
                displayName: userName,
                onTap: () async {
                  final result = await Navigator.of(context).push<bool>(
                    MaterialPageRoute(
                      builder: (context) => LeaveDetailsScreen(
                        leave: filteredLeaves[index],
                        displayName: userName ?? 'User',
                      ),
                    ),
                  );
                  // Refresh if leave was cancelled
                  if (result == true && context.mounted) {
                    context.read<LeavesCubit>().refresh();
                  }
                },
              ),
            );
          },
        ),
      ),
    );
  }
}
