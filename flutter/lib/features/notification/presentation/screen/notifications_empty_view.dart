import 'package:flutter/material.dart';

import '../../../../core/components/spacer.dart';
import '../../../../core/components/style.dart';
import '../../../../core/utiles/app_colors.dart';
import '../../../../l10n/app_localizations.dart';

class NotificationsEmptyView extends StatelessWidget {
  const NotificationsEmptyView({super.key});

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;

    return Center(
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 22),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            SizedBox(
              width: 110,
              height: 110,
              child: Center(
                child: Image.asset(
                  'assets/images/notification.png',
                  width: 150,
                  height: 150,
                  fit: BoxFit.contain,
                ),
              ),
            ),
            verticalSpace(height: 18),
            Text(
              l10n.noNotificationsYet,
              textAlign: TextAlign.center,
              style: Styles.textTitle16Weight600.copyWith(
                color: AppColors.whiteColor,
              ),
            ),
            verticalSpace(height: 8),
            Text(
              l10n.notificationsEmptyHint,
              textAlign: TextAlign.center,
              style: Styles.textTitle12Weight500.copyWith(
                color: AppColors.unSelectedColor,
                height: 1.35,
              ),
            ),
          ],
        ),
      ),
    );
  }

}
