import 'package:flutter/material.dart';

import '../../../../core/utiles/app_colors.dart';
import '../../data/models/leave_status_model.dart';

/// Widget displaying status filter chips including "All" option
class StatusFilterChips extends StatelessWidget {
  final List<LeaveStatusModel> statuses;
  final String? selectedStatus;
  final ValueChanged<String?> onSelected;
  final bool isLoading;

  const StatusFilterChips({
    super.key,
    required this.statuses,
    required this.selectedStatus,
    required this.onSelected,
    this.isLoading = false,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isDark = theme.brightness == Brightness.dark;

    if (isLoading) {
      return const SizedBox(
        height: 40,
        child: Center(child: CircularProgressIndicator(strokeWidth: 2)),
      );
    }

    // Create list with "All" at the beginning
    final allStatuses = [
      null, // represents "All"
      ...statuses.map((s) => s.name),
    ];

    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      padding: const EdgeInsets.symmetric(horizontal: 16),
      child: Row(
        children: allStatuses.map((status) {
          final isSelected = selectedStatus == status;
          final label = status ?? 'All';

          return Padding(
            padding: const EdgeInsets.only(right: 8),
            child: ChoiceChip(
              label: Text(label),
              selected: isSelected,
              onSelected: (_) => onSelected(status),
              selectedColor: AppColors.primary.withValues(alpha: 0.2),
              backgroundColor: isDark
                  ? AppColors.primary.withValues(alpha: 0.08)
                  : AppColors.primary.withValues(alpha: 0.05),
              labelStyle: TextStyle(
                color: isSelected
                    ? AppColors.primary
                    : isDark
                        ? AppColors.primary.withValues(alpha: 0.8)
                        : AppColors.primary.withValues(alpha: 0.7),
                fontWeight: isSelected ? FontWeight.w600 : FontWeight.w500,
              ),
              side: BorderSide(
                color: isSelected
                    ? AppColors.primary
                    : AppColors.primary.withValues(alpha: 0.3),
                width: isSelected ? 1.5 : 1.0,
              ),
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(20),
              ),
            ),
          );
        }).toList(),
      ),
    );
  }
}
