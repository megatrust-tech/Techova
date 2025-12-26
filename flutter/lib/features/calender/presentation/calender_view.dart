import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import 'package:taskedin_flutter/features/calender/presentation/widgets/calender_leave_card.dart';
import 'package:taskedin_flutter/features/calender/presentation/widgets/full_time_avilable_card.dart';

import '../../../core/servicelocator/servicelocator.dart';
import '../../../core/utiles/app_colors.dart';
import '../../../l10n/app_localizations.dart';

import '../models/paginated_leave_response.dart';
import 'cubit/calendar_cubit.dart';
import 'cubit/calendar_state.dart';

class CalendarView extends StatelessWidget {
  const CalendarView({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocProvider(
      create: (_) => sl<CalendarCubit>()..fetchRecentLeaveRequests(pageNumber: 1),
      child: const _CalendarBody(),
    );
  }
}

class _CalendarBody extends StatelessWidget {
  const _CalendarBody();

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;

    return BlocBuilder<CalendarCubit, CalendarState>(
      builder: (context, state) {
        final cubit = context.read<CalendarCubit>();
        final List<LeaveRequestModels> leaves = cubit.allLeaves;

        final now = DateTime.now();
        final today = DateTime(now.year, now.month, now.day);
        final tomorrow = today.add(const Duration(days: 1));
        final nextWeekEnd = today.add(const Duration(days: 7));

        bool inRange(DateTime d, DateTime start, DateTime end) {
          final x = DateTime(d.year, d.month, d.day);
          return (x.isAtSameMomentAs(start) || x.isAfter(start)) &&
              (x.isAtSameMomentAs(end) || x.isBefore(end));
        }

        bool leaveHappensOn(LeaveRequestModels l, DateTime day) {
          final s = l.startDate;
          final e = l.endDate;
          if (s == null || e == null) return false;

          return inRange(
            day,
            DateTime(s.year, s.month, s.day),
            DateTime(e.year, e.month, e.day),
          );
        }

        final todayLeaves = leaves.where((l) => leaveHappensOn(l, today)).toList();
        final tomorrowLeaves = leaves.where((l) => leaveHappensOn(l, tomorrow)).toList();

        final nextWeekLeaves = leaves.where((l) {
          final s = l.startDate;
          if (s == null) return false;
          final sd = DateTime(s.year, s.month, s.day);

          return sd.isAfter(tomorrow) &&
              inRange(sd, tomorrow.add(const Duration(days: 1)), nextWeekEnd);
        }).toList();

        final isLoading = state is CalendarLoading;
        final errorText = state is CalendarFailure ? state.error : null;

        return Scaffold(
          backgroundColor: Theme.of(context).scaffoldBackgroundColor,
          body: SafeArea(
            child: SingleChildScrollView(
              child: Padding(
                padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 20),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Text(
                          l10n.teamCalendar,
                          style: TextStyle(
                            color: AppColors.textColor,
                            fontSize: 20,
                            fontWeight: FontWeight.w800,
                          ),
                        ),
                        const Spacer(),
                        IconButton(
                          onPressed: () => cubit.refreshDashboard(),
                          icon: Icon(Icons.refresh, color: AppColors.textColor),
                        ),
                      ],
                    ),
                    const SizedBox(height: 12),

                    if (errorText != null) ...[
                      Text(errorText, style: TextStyle(color: AppColors.error)),
                      const SizedBox(height: 12),
                    ],

                    if (isLoading && leaves.isEmpty)
                      Center(
                        child: CircularProgressIndicator(color: AppColors.primary),
                      ),

                    _section(l10n.today, today, todayLeaves, l10n),
                    const SizedBox(height: 18),

                    _section(l10n.tomorrow, tomorrow, tomorrowLeaves, l10n),
                    const SizedBox(height: 18),

                    _section(l10n.nextWeek, nextWeekEnd, nextWeekLeaves, l10n),
                  ],
                ),
              ),
            ),
          ),
        );
      },
    );
  }

  Widget _section(
      String title,
      DateTime dateForLeft,
      List<LeaveRequestModels> items,
      AppLocalizations l10n,
      ) {
    final date = dateForLeft.day.toString();
    final day = _weekdayShort(dateForLeft.weekday, l10n);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          title,
          style: TextStyle(
            color: AppColors.textColor,
            fontSize: 16,
            fontWeight: FontWeight.w700,
          ),
        ),
        const SizedBox(height: 12),
        if (items.isEmpty)
          CalendarRowItem(
            date: date,
            day: day,
            child: const FullTeamAvailableCard(),
          )
        else
          Column(
            children: items.take(3).map((leave) {
              return Padding(
                padding: const EdgeInsets.only(bottom: 12),
                child: CalendarRowItem(
                  date: date,
                  day: day,
                  child: CalendarLeaveCard(leave: leave),
                ),
              );
            }).toList(),
          ),
      ],
    );
  }

  static String _weekdayShort(int weekday, AppLocalizations l10n) {
    switch (weekday) {
      case DateTime.monday:
        return l10n.dayMon;
      case DateTime.tuesday:
        return l10n.dayTue;
      case DateTime.wednesday:
        return l10n.dayWed;
      case DateTime.thursday:
        return l10n.dayThu;
      case DateTime.friday:
        return l10n.dayFri;
      case DateTime.saturday:
        return l10n.daySat;
      default:
        return l10n.daySun;
    }
  }
}

class CalendarRowItem extends StatelessWidget {
  final String date;
  final String day;
  final Widget child;

  const CalendarRowItem({
    super.key,
    required this.date,
    required this.day,
    required this.child,
  });

  @override
  Widget build(BuildContext context) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Column(
          children: [
            Text(
              date,
              style: TextStyle(
                color: AppColors.textColor,
                fontSize: 14,
                fontWeight: FontWeight.w700,
              ),
            ),
            const SizedBox(height: 3),
            Text(
              day,
              style: TextStyle(
                color: AppColors.unSelectedColor,
                fontSize: 12,
                fontWeight: FontWeight.w600,
              ),
            ),
          ],
        ),
        const SizedBox(width: 21),
        Expanded(child: child),
      ],
    );
  }
}
