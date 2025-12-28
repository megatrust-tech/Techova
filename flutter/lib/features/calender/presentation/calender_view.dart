import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:iconly/iconly.dart';
import 'package:table_calendar/table_calendar.dart';

import '../../../core/servicelocator/servicelocator.dart';
import '../../../core/utiles/app_colors.dart';
import '../../../l10n/app_localizations.dart';
import '../data/repo/calendar_repo.dart';
import '../models/calendar_data_models.dart';
import 'cubit/calendar_cubit.dart';
import 'cubit/calendar_state.dart';
import 'widgets/calendar_leave_item_card.dart';

class CalendarView extends StatelessWidget {
  const CalendarView({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocProvider(
      create: (_) => CalendarCubit(sl<CalendarRepository>())..loadCalendarData(DateTime.now()),
      child: const _CalendarBody(),
    );
  }
}

class _CalendarBody extends StatelessWidget {
  const _CalendarBody();

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Scaffold(
      backgroundColor: Theme.of(context).scaffoldBackgroundColor,
      body: SafeArea(
        child: BlocBuilder<CalendarCubit, CalendarState>(
          builder: (context, state) {
            final cubit = context.read<CalendarCubit>();

            if (state is CalendarLoading) {
              return Center(
                child: CircularProgressIndicator(color: AppColors.primary),
              );
            }

            if (state is CalendarFailure) {
              return _buildErrorView(context, state.error, cubit);
            }

            if (state is CalendarDataLoaded) {
              return _buildCalendarView(context, state, cubit, l10n, isDark);
            }

            // Initial state - trigger load
            return Center(
              child: CircularProgressIndicator(color: AppColors.primary),
            );
          },
        ),
      ),
    );
  }

  Widget _buildErrorView(BuildContext context, String error, CalendarCubit cubit) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.error_outline, size: 48, color: AppColors.error),
            const SizedBox(height: 16),
            Text(
              error,
              textAlign: TextAlign.center,
              style: TextStyle(color: AppColors.error),
            ),
            const SizedBox(height: 16),
            ElevatedButton(
              onPressed: () => cubit.loadCalendarData(DateTime.now()),
              child: const Text('Retry'),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildCalendarView(
    BuildContext context,
    CalendarDataLoaded state,
    CalendarCubit cubit,
    AppLocalizations l10n,
    bool isDark,
  ) {
    final datesWithLeaves = state.data.datesWithLeaves;
    final selectedLeaves = state.selectedDay != null
        ? state.data.getLeavesForDate(state.selectedDay!)
        : <CalendarLeaveDto>[];

    return RefreshIndicator(
      onRefresh: () => cubit.loadCalendarData(state.focusedDay),
      child: SingleChildScrollView(
        physics: const AlwaysScrollableScrollPhysics(),
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Header
              Row(
                children: [
                  Text(
                    l10n.teamCalendar,
                    style: TextStyle(
                      color: isDark ? AppColors.darkText : AppColors.lightText,
                      fontSize: 20,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                  const Spacer(),
                  IconButton(
                    onPressed: () => cubit.loadCalendarData(DateTime.now()),
                    icon: Icon(
                      IconlyLight.calendar,
                      color: isDark ? AppColors.darkText : AppColors.lightText,
                    ),
                    tooltip: 'Go to Today',
                  ),
                ],
              ),
              const SizedBox(height: 16),

              // Table Calendar
              Container(
                decoration: BoxDecoration(
                  color: isDark ? AppColors.darkSurface : Colors.white,
                  borderRadius: BorderRadius.circular(16),
                  boxShadow: [
                    BoxShadow(
                      color: Colors.black.withValues(alpha: 0.05),
                      blurRadius: 10,
                      offset: const Offset(0, 4),
                    ),
                  ],
                ),
                child: TableCalendar<CalendarLeaveDto>(
                  firstDay: DateTime(2020, 1, 1),
                  lastDay: DateTime(2030, 12, 31),
                  focusedDay: state.focusedDay,
                  selectedDayPredicate: (day) =>
                      state.selectedDay != null && isSameDay(state.selectedDay!, day),
                  calendarFormat: CalendarFormat.month,
                  startingDayOfWeek: StartingDayOfWeek.monday,
                  headerStyle: HeaderStyle(
                    formatButtonVisible: false,
                    titleCentered: true,
                    titleTextStyle: TextStyle(
                      fontSize: 16,
                      fontWeight: FontWeight.w700,
                      color: isDark ? AppColors.darkText : AppColors.lightText,
                    ),
                    leftChevronIcon: Icon(
                      Icons.chevron_left,
                      color: AppColors.primary,
                    ),
                    rightChevronIcon: Icon(
                      Icons.chevron_right,
                      color: AppColors.primary,
                    ),
                  ),
                  daysOfWeekStyle: DaysOfWeekStyle(
                    weekdayStyle: TextStyle(
                      color: isDark ? AppColors.darkNavInactive : AppColors.lightNavInactive,
                      fontWeight: FontWeight.w600,
                      fontSize: 12,
                    ),
                    weekendStyle: TextStyle(
                      color: isDark ? AppColors.darkNavInactive : AppColors.lightNavInactive,
                      fontWeight: FontWeight.w600,
                      fontSize: 12,
                    ),
                  ),
                  calendarStyle: CalendarStyle(
                    defaultTextStyle: TextStyle(
                      color: isDark ? AppColors.darkText : AppColors.lightText,
                    ),
                    weekendTextStyle: TextStyle(
                      color: isDark ? AppColors.darkText : AppColors.lightText,
                    ),
                    outsideTextStyle: TextStyle(
                      color: (isDark ? AppColors.darkNavInactive : AppColors.lightNavInactive)
                          .withValues(alpha: 0.5),
                    ),
                    selectedDecoration: BoxDecoration(
                      color: AppColors.primary,
                      shape: BoxShape.circle,
                    ),
                    todayDecoration: BoxDecoration(
                      color: AppColors.primary.withValues(alpha: 0.3),
                      shape: BoxShape.circle,
                    ),
                    todayTextStyle: TextStyle(
                      color: isDark ? AppColors.darkText : AppColors.lightText,
                      fontWeight: FontWeight.bold,
                    ),
                    markerDecoration: BoxDecoration(
                      color: AppColors.warning,
                      shape: BoxShape.circle,
                    ),
                    markerSize: 6,
                    markersMaxCount: 3,
                  ),
                  eventLoader: (day) {
                    final dateOnly = DateTime(day.year, day.month, day.day);
                    return datesWithLeaves.contains(dateOnly)
                        ? [CalendarLeaveDto(
                            id: 0,
                            employeeId: 0,
                            employeeName: '',
                            leaveType: '',
                            startDate: day,
                            endDate: day,
                            numberOfDays: 1,
                          )]
                        : [];
                  },
                  onDaySelected: (selectedDay, focusedDay) {
                    cubit.onDateSelected(selectedDay, focusedDay);
                  },
                  onPageChanged: (focusedDay) {
                    cubit.onMonthChanged(focusedDay);
                  },
                ),
              ),

              const SizedBox(height: 24),

              // Selected Day Leaves
              if (state.selectedDay != null) ...[
                Text(
                  _formatSelectedDate(state.selectedDay!, l10n),
                  style: TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.w700,
                    color: isDark ? AppColors.darkText : AppColors.lightText,
                  ),
                ),
                const SizedBox(height: 12),
                if (selectedLeaves.isEmpty)
                  _buildNoLeavesCard(isDark, l10n)
                else
                  ...selectedLeaves.map((leave) => Padding(
                        padding: const EdgeInsets.only(bottom: 12),
                        child: CalendarLeaveItemCard(
                          leave: leave,
                          showManagerName: state.data.isGrouped,
                        ),
                      )),
              ],

              // Grouped by Manager (for HR/Admin)
              if (state.data.isGrouped && state.selectedDay == null) ...[
                Text(
                  'Select a date to view leaves',
                  style: TextStyle(
                    fontSize: 14,
                    color: isDark ? AppColors.darkNavInactive : AppColors.lightNavInactive,
                  ),
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildNoLeavesCard(bool isDark, AppLocalizations l10n) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: isDark ? AppColors.darkSurface : Colors.white,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(
          color: AppColors.success.withValues(alpha: 0.3),
          width: 2,
        ),
      ),
      child: Row(
        children: [
          Container(
            width: 6,
            height: 40,
            decoration: BoxDecoration(
              color: AppColors.success,
              borderRadius: BorderRadius.circular(3),
            ),
          ),
          const SizedBox(width: 12),
          Icon(Icons.check_circle, color: AppColors.success),
          const SizedBox(width: 10),
          Text(
            l10n.fullTeamAvailable,
            style: TextStyle(
              color: isDark ? AppColors.darkText : AppColors.lightText,
              fontSize: 14,
              fontWeight: FontWeight.w600,
            ),
          ),
        ],
      ),
    );
  }

  String _formatSelectedDate(DateTime date, AppLocalizations l10n) {
    final months = [
      'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
      'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'
    ];
    return '${months[date.month - 1]} ${date.day}, ${date.year}';
  }
}
