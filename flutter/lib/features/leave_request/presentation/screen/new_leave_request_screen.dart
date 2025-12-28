import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../../core/servicelocator/servicelocator.dart';
import '../../../../core/utiles/app_colors.dart';
import '../../../../l10n/app_localizations.dart';
import '../../data/repositories/leave_repository.dart';
import '../cubit/leave_request_cubit.dart';
import '../widgets/conflict_widget.dart';
import '../widgets/date_range_selector.dart';
import '../widgets/file_upload_section.dart';
import '../widgets/leave_type_chips.dart';

/// Screen for creating a new leave request
class NewLeaveRequestScreen extends StatelessWidget {
  const NewLeaveRequestScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocProvider(
      create: (context) => LeaveRequestCubit(sl<LeaveRepository>())..initialize(),
      child: const _NewLeaveRequestView(),
    );
  }
}

class _NewLeaveRequestView extends StatelessWidget {
  const _NewLeaveRequestView();

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final l10n = AppLocalizations.of(context)!;

    // Define a shared decoration to ensure exact consistency across all fields
    final baseInputDecoration = InputDecoration(
      border: OutlineInputBorder(
        borderRadius: BorderRadius.circular(12),
      ),
      enabledBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(12),
        borderSide: BorderSide(
          color: Colors.grey.withValues(alpha: 0.3),
        ),
      ),
      focusedBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(12),
        borderSide: BorderSide(
          color: AppColors.primary,
          width: 1.5,
        ),
      ),
      // contentPadding: EdgeInsets.zero, // Uncomment if inner widgets have their own padding
    );

    return Scaffold(
      appBar: AppBar(
        title: Text(l10n.applyLeave),
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () => Navigator.of(context).pop(),
        ),
        elevation: 0,
        backgroundColor: theme.scaffoldBackgroundColor,
      ),
      body: BlocConsumer<LeaveRequestCubit, LeaveRequestState>(
        listener: (context, state) {
          if (state is LeaveRequestFormState) {
            if (state.submitSuccess) {
              ScaffoldMessenger.of(context).showSnackBar(
                SnackBar(
                  content: Text(l10n.submit),
                  backgroundColor: AppColors.success,
                ),
              );
              Navigator.of(context).pop(true);
            } else if (state.errorMessage != null) {
              ScaffoldMessenger.of(context).showSnackBar(
                SnackBar(
                  content: Text(state.errorMessage!),
                  backgroundColor: AppColors.error,
                ),
              );
            }
          }
        },
        builder: (context, state) {
          if (state is LeaveRequestInitial) {
            return const Center(child: CircularProgressIndicator());
          }

          if (state is! LeaveRequestFormState) {
            return const Center(child: CircularProgressIndicator());
          }

          return SingleChildScrollView(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Section 1: Leave Type
                _SectionHeader(title: l10n.reason),
                const SizedBox(height: 12),
                LeaveTypeChips(
                  leaveTypes: state.leaveTypes,
                  selectedType: state.selectedLeaveType,
                  onSelected: (type) {
                    context.read<LeaveRequestCubit>().selectLeaveType(type);
                  },
                  isLoading: state.isLoadingLeaveTypes,
                ),
                const SizedBox(height: 24),

                // Section 2: Duration
                _SectionHeader(title: l10n.startDate),
                const SizedBox(height: 12),
                InputDecorator(
                  decoration: baseInputDecoration,
                  child: DateRangeSelector(
                    startDate: state.startDate,
                    endDate: state.endDate,
                    numberOfDays: state.numberOfDays,
                    isLoading: state.isCheckingConflict,
                    onTap: () => _showDateRangePicker(context, state),
                  ),
                ),
                const SizedBox(height: 16),

                // Conflict Widget
                if (state.conflictResult != null &&
                    state.conflictResult!.hasConflict) ...[
                  ConflictWidget(message: state.conflictResult!.message),
                  const SizedBox(height: 24),
                ] else if (state.conflictResult != null &&
                    !state.conflictResult!.hasConflict) ...[
                  _NoConflictBadge(),
                  const SizedBox(height: 24),
                ] else
                  const SizedBox(height: 8),

                // Section 3: Notes
                _SectionHeader(title: l10n.notes, optional: true),
                const SizedBox(height: 12),
                TextField(
                  onChanged: (value) {
                    context.read<LeaveRequestCubit>().updateNotes(value);
                  },
                  maxLines: 3,
                  decoration: baseInputDecoration.copyWith(
                    hintText: 'Add any additional notes...',
                  ),
                ),
                const SizedBox(height: 24),

                // Section 4: Attachment
                _SectionHeader(title: 'Attachment', optional: true),
                const SizedBox(height: 12),
                InputDecorator(
                  decoration: baseInputDecoration,
                  child: FileUploadSection(
                    selectedFile: state.attachment,
                    onSelectFile: () async {
                      final file = await FileUploadSection.pickFile();
                      if (file != null && context.mounted) {
                        context.read<LeaveRequestCubit>().setAttachment(file);
                      }
                    },
                    onRemoveFile: () {
                      context.read<LeaveRequestCubit>().removeAttachment();
                    },
                  ),
                ),
                const SizedBox(height: 32),

                // Submit/Cancel Buttons
                Row(
                  children: [
                    Expanded(
                      child: OutlinedButton(
                        onPressed: () => Navigator.of(context).pop(),
                        style: OutlinedButton.styleFrom(
                          padding: const EdgeInsets.symmetric(vertical: 16),
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(12),
                          ),
                          side: BorderSide(color: Colors.grey.shade400),
                        ),
                        child: Text(l10n.cancel),
                      ),
                    ),
                    const SizedBox(width: 16),
                    Expanded(
                      flex: 2,
                      child: ElevatedButton(
                        onPressed: state.canSubmit
                            ? () {
                          context
                              .read<LeaveRequestCubit>()
                              .submitLeaveRequest();
                        }
                            : null,
                        style: ElevatedButton.styleFrom(
                          backgroundColor: AppColors.primary,
                          foregroundColor: Colors.white,
                          padding: const EdgeInsets.symmetric(vertical: 16),
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(12),
                          ),
                          disabledBackgroundColor:
                          AppColors.primary.withValues(alpha: 0.3),
                        ),
                        child: state.isSubmitting
                            ? const SizedBox(
                          width: 20,
                          height: 20,
                          child: CircularProgressIndicator(
                            strokeWidth: 2,
                            color: Colors.white,
                          ),
                        )
                            : Text(l10n.submit),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 24),
              ],
            ),
          );
        },
      ),
    );
  }

  Future<void> _showDateRangePicker(
      BuildContext context,
      LeaveRequestFormState state,
      ) async {
    final theme = Theme.of(context);
    final now = DateTime.now();

    final result = await showDateRangePicker(
      context: context,
      firstDate: now,
      lastDate: now.add(const Duration(days: 365)),
      initialDateRange: state.startDate != null && state.endDate != null
          ? DateTimeRange(start: state.startDate!, end: state.endDate!)
          : null,
      builder: (context, child) {
        return Theme(
          data: theme.copyWith(
            colorScheme: theme.colorScheme.copyWith(
              primary: AppColors.primary,
              onPrimary: Colors.white,
              surface: theme.scaffoldBackgroundColor,
              onSurface: theme.colorScheme.onSurface,
            ),
          ),
          child: child!,
        );
      },
    );

    if (result != null && context.mounted) {
      context.read<LeaveRequestCubit>().setDateRange(result.start, result.end);
    }
  }
}

class _SectionHeader extends StatelessWidget {
  final String title;
  final bool optional;

  const _SectionHeader({
    required this.title,
    this.optional = false,
  });

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Text(
          title,
          style: Theme.of(context).textTheme.titleMedium?.copyWith(
            fontWeight: FontWeight.w600,
          ),
        ),
        if (optional) ...[
          const SizedBox(width: 4),
          Text(
            '(Optional)',
            style: Theme.of(context).textTheme.bodySmall?.copyWith(
              color: Colors.grey,
            ),
          ),
        ],
      ],
    );
  }
}

class _NoConflictBadge extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
      decoration: BoxDecoration(
        color: AppColors.success.withValues(alpha: 0.1),
        borderRadius: BorderRadius.circular(8),
        border: Border.all(
          color: AppColors.success.withValues(alpha: 0.5),
        ),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(
            Icons.check_circle,
            color: AppColors.success,
            size: 18,
          ),
          const SizedBox(width: 8),
          Text(
            'No conflicts found',
            style: TextStyle(
              color: AppColors.success,
              fontWeight: FontWeight.w500,
              fontSize: 13,
            ),
          ),
        ],
      ),
    );
  }
}