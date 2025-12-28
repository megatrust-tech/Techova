import 'package:flutter/material.dart';

import '../../data/models/leave_type_model.dart';

/// Widget displaying selectable leave type chips
class LeaveTypeChips extends StatelessWidget {
  final List<LeaveTypeModel> leaveTypes;
  final LeaveTypeModel? selectedType;
  final ValueChanged<LeaveTypeModel> onSelected;
  final bool isLoading;

  const LeaveTypeChips({
    super.key,
    required this.leaveTypes,
    required this.selectedType,
    required this.onSelected,
    this.isLoading = false,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isDark = theme.brightness == Brightness.dark;

    if (isLoading) {
      return const Center(
        child: Padding(
          padding: EdgeInsets.all(16.0),
          child: CircularProgressIndicator(),
        ),
      );
    }

    if (leaveTypes.isEmpty) {
      return Padding(
        padding: const EdgeInsets.all(16.0),
        child: Text(
          'No leave types available',
          style: theme.textTheme.bodyMedium?.copyWith(
            color: theme.colorScheme.onSurface.withValues(alpha: 0.6),
          ),
        ),
      );
    }

    return Wrap(
      spacing: 8.0,
      runSpacing: 8.0,
      children: leaveTypes.map((type) {
        final isSelected = selectedType?.value == type.value;
        return ChoiceChip(
          label: Text(type.name),
          selected: isSelected,
          onSelected: (_) => onSelected(type),
          selectedColor: theme.colorScheme.primary.withValues(alpha: 0.2),
          backgroundColor: isDark
              ? theme.colorScheme.primary.withValues(alpha: 0.08)
              : theme.colorScheme.primary.withValues(alpha: 0.05),
          labelStyle: TextStyle(
            color: isSelected
                ? theme.colorScheme.primary
                : isDark
                    ? theme.colorScheme.primary.withValues(alpha: 0.8)
                    : theme.colorScheme.primary.withValues(alpha: 0.7),
            fontWeight: isSelected ? FontWeight.w600 : FontWeight.w500,
          ),
          side: BorderSide(
            color: isSelected
                ? theme.colorScheme.primary
                : theme.colorScheme.primary.withValues(alpha: 0.3),
            width: isSelected ? 1.5 : 1.0,
          ),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(20),
          ),
        );
      }).toList(),
    );
  }
}
