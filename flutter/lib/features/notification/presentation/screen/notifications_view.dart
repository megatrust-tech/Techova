import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../../core/components/spacer.dart';
import '../../../../core/components/style.dart';
import '../../../../core/utiles/app_colors.dart';
import '../../../../l10n/app_localizations.dart';

import '../../data/data_sources/notification_dto.dart';
import '../cubit/notifications_cubit.dart';
import '../cubit/notifications_state.dart';
import '../widgets/notification_item_tile.dart';
import 'notifications_empty_view.dart';

class NotificationsView extends StatefulWidget {
  const NotificationsView({super.key});

  @override
  State<NotificationsView> createState() => _NotificationsViewState();
}

class _NotificationsViewState extends State<NotificationsView> {
  @override
  void initState() {
    super.initState();
    context.read<NotificationsCubit>().load(pageNumber: 1, pageSize: 20);
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;

    return Scaffold(
      backgroundColor: AppColors.backgroundColor,
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 16),
          child: BlocConsumer<NotificationsCubit, NotificationsState>(
            listener: (context, state) {
              if (state is NotificationsError) {
                ScaffoldMessenger.of(context).showSnackBar(
                  SnackBar(
                    content: Text(
                      state.message,
                      style: Styles.textTitle14Weight400.copyWith(
                        color: Colors.white,
                      ),
                    ),
                    backgroundColor: Colors.red,
                  ),
                );
              }
            },
            builder: (context, state) {
              final cubit = context.read<NotificationsCubit>();

              bool loading = false;
              String? errorText;
              List<NotificationDto> items = const [];

              if (state is NotificationsLoading) {
                loading = true;
              } else if (state is NotificationsSuccess) {
                items = state.items;
              } else if (state is NotificationsError) {
                errorText = state.message;
                items = state.cached ?? const [];
              } else if (state is NotificationsMarkingRead) {
                items = state.cached;
              } else if (state is NotificationsMarkAllReadLoading) {
                items = state.cached;
              }

              final hasItems = items.isNotEmpty;

              return Column(
                children: [
                  _Header(
                    title: l10n.notifications,
                    markAsReadText: l10n.markAsRead,
                    onBack: () => Navigator.pop(context),
                    onMarkAsRead: hasItems ? cubit.markAllAsRead : null,
                  ),
                  verticalSpace(height: 18),
                  if (loading)
                    Expanded(
                      child: Center(
                        child: CircularProgressIndicator(
                          color: AppColors.primaryColor,
                        ),
                      ),
                    )
                  else if (errorText != null && !hasItems)
                    Expanded(
                      child: Center(
                        child: Text(
                          errorText,
                          style: Styles.textTitle14Weight500.copyWith(
                            color: AppColors.whiteColor,
                          ),
                          textAlign: TextAlign.center,
                        ),
                      ),
                    )
                  else
                    Expanded(
                      child: hasItems
                          ? RefreshIndicator(
                        color: AppColors.primaryColor,
                        onRefresh: () =>
                            cubit.load(pageNumber: 1, pageSize: 20),
                        child: ListView.separated(
                          itemCount: items.length,
                          separatorBuilder: (_, __) =>
                              verticalSpace(height: 12),
                          itemBuilder: (context, index) {
                            final n = items[index];
                            return NotificationItemTile(
                              item: n,
                              onTap: () => cubit.markAsRead(n.id),
                            );
                          },
                        ),
                      )
                          : const NotificationsEmptyView(),
                    ),
                ],
              );
            },
          ),
        ),
      ),
    );
  }
}

class _Header extends StatelessWidget {
  final VoidCallback onBack;
  final VoidCallback? onMarkAsRead;

  final String title;
  final String markAsReadText;

  const _Header({
    required this.onBack,
    required this.onMarkAsRead,
    required this.title,
    required this.markAsReadText,
  });

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      height: 48,
      child: Row(
        children: [
          InkWell(
            onTap: onBack,
            borderRadius: BorderRadius.circular(14),
            child: Container(
              width: 44,
              height: 44,
              decoration: BoxDecoration(
                color: AppColors.mainColor,
                borderRadius: BorderRadius.circular(14),
                border: Border.all(
                  color: AppColors.unSelectedColor.withOpacity(.35),
                ),
              ),
              child: Icon(
                Icons.arrow_back_ios_new_rounded,
                size: 18,
                color: AppColors.whiteColor,
              ),
            ),
          ),
          Expanded(
            child: Center(
              child: Text(
                title,
                style: Styles.textTitle14Weight600.copyWith(
                  color: AppColors.whiteColor,
                ),
              ),
            ),
          ),
          TextButton(
            onPressed: onMarkAsRead,
            child: Text(
              markAsReadText,
              style: Styles.textTitle12Weight500.copyWith(
                color: onMarkAsRead == null
                    ? AppColors.unSelectedColor
                    : AppColors.primaryColor,
                decoration: TextDecoration.underline,
                decorationColor: onMarkAsRead == null
                    ? AppColors.unSelectedColor
                    : AppColors.primaryColor,
              ),
            ),
          ),
        ],
      ),
    );
  }
}
