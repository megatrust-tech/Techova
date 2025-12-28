import 'package:flutter/material.dart';

import '../../../../core/utiles/app_colors.dart';
import '../../../../l10n/app_localizations.dart';
import '../../models/paginated_leave_response.dart';
import 'custom_card.dart';

class CalendarLeaveCard extends StatelessWidget {
  const CalendarLeaveCard({super.key, required this.leave});

  final LeaveRequestModels leave;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    final isDark = Theme.of(context).brightness == Brightness.dark;

    final statusColor = _statusColor(leave.status);
    final statusText = _statusText(leave.status, l10n);

    return CustomCard(
      child: ClipRRect(
        borderRadius: BorderRadius.circular(16),
        child: IntrinsicHeight(
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Container(width: 6, color: statusColor),
              Expanded(
                child: Padding(
                  padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Row(
                        children: [
                          CircleAvatar(
                            radius: 18,
                            backgroundColor: (isDark ? AppColors.darkNavInactive : AppColors.lightNavInactive).withOpacity(.2),
                            child: Icon(Icons.person, color: isDark ? AppColors.darkText : AppColors.lightText, size: 18),
                          ),
                          const SizedBox(width: 12),
                          Expanded(
                            child: Text(
                              leave.employeeEmail ?? '—',
                              maxLines: 1,
                              overflow: TextOverflow.ellipsis,
                              style: TextStyle(
                                color: isDark ? AppColors.darkText : AppColors.lightText,
                                fontSize: 13,
                                fontWeight: FontWeight.w600,
                              ),
                            ),
                          ),
                          Container(
                            padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
                            decoration: BoxDecoration(
                              color: statusColor.withOpacity(.12),
                              borderRadius: BorderRadius.circular(999),
                              border: Border.all(color: statusColor),
                            ),
                            child: Text(
                              statusText,
                              style: TextStyle(
                                color: statusColor,
                                fontSize: 10,
                                fontWeight: FontWeight.w700,
                              ),
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 12),
                      _row(l10n.leaveTypeLabel, leave.leaveType ?? '—', isDark),
                      _row(l10n.leaveStartLabel, _fmt(leave.startDate), isDark),
                      _row(l10n.leaveEndLabel, _fmt(leave.endDate), isDark),
                      if ((leave.notes ?? '').trim().isNotEmpty)
                        _row(l10n.leaveNotesLabel, (leave.notes ?? '').trim(), isDark),
                    ],
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _row(String k, String v, bool isDark) {
    return Padding(
      padding: const EdgeInsets.only(top: 6),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 70,
            child: Text(
              '$k:',
              style: TextStyle(
                color: isDark ? AppColors.darkNavInactive : AppColors.lightNavInactive,
                fontSize: 12,
                fontWeight: FontWeight.w600,
              ),
            ),
          ),
          Expanded(
            child: Text(
              v,
              style: TextStyle(
                color: isDark ? AppColors.darkText : AppColors.lightText,
                fontSize: 12,
                fontWeight: FontWeight.w600,
              ),
            ),
          ),
        ],
      ),
    );
  }

  static String _fmt(DateTime? d) {
    if (d == null) return '—';
    return '${d.year}-${_two(d.month)}-${_two(d.day)}';
  }

  static String _two(int v) => v < 10 ? '0$v' : '$v';

  static String _normalizeStatus(String? s) {
    final v = (s ?? '').toLowerCase();
    if (v.contains('approve')) return 'approved';
    if (v.contains('reject')) return 'rejected';
    if (v.contains('pending')) return 'pending';
    if (v.contains('cancel')) return 'cancelled';
    return v;
  }

  static String _statusText(String? s, AppLocalizations l10n) {
    switch (_normalizeStatus(s)) {
      case 'approved':
        return l10n.leaveApproved;
      case 'rejected':
        return l10n.leaveRejected;
      case 'pending':
        return l10n.leavePending;
      case 'cancelled':
        return l10n.leaveCanceled;
      default:
        return (s ?? '—');
    }
  }

  static Color _statusColor(String? s) {
    switch (_normalizeStatus(s)) {
      case 'approved':
        return const Color(0xff34C759);
      case 'rejected':
        return const Color(0xffFF383C);
      case 'pending':
        return const Color(0xffFF8D28);
      case 'cancelled':
        return const Color(0xffFF383C);
      default:
        return Colors.grey;
    }
  }
}
