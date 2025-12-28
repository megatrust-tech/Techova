import 'package:flutter/material.dart';

import '../../../../core/utiles/app_colors.dart';
import '../../../../l10n/app_localizations.dart';
import 'custom_card.dart';

class FullTeamAvailableCard extends StatelessWidget {
  const FullTeamAvailableCard({super.key});

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return CustomCard(
      child: ClipRRect(
        borderRadius: BorderRadius.circular(16),
        child: IntrinsicHeight(
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Container(width: 6, color: AppColors.success),
              Expanded(
                child: Padding(
                  padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 18),
                  child: Row(
                    children: [
                      Icon(Icons.check_circle, color: AppColors.success),
                      const SizedBox(width: 10),
                      Text(
                        l10n.fullTeamAvailable,
                        style: TextStyle(
                          color: isDark ? AppColors.darkText : AppColors.lightText,
                          fontSize: 16,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
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
}
