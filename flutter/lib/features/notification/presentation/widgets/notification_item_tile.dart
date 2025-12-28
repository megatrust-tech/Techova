import 'package:flutter/material.dart';
import 'package:flutter_svg/svg.dart';
import '../../../../core/components/spacer.dart';
import '../../../../core/components/style.dart';
import '../../../../core/utiles/app_colors.dart';
import '../../data/data_sources/notification_dto.dart';

class NotificationItemTile extends StatelessWidget {
  final NotificationDto item;
  final VoidCallback? onTap;

  const NotificationItemTile({super.key, required this.item, this.onTap});

  @override
  Widget build(BuildContext context) {
    final bg = AppColors.mainColor;
    final border = AppColors.unSelectedColor.withOpacity(.35);

    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(14),
      child: Opacity(
        opacity: item.isRead ? 0.55 : 1,
        child: Container(
          padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
          decoration: BoxDecoration(
            color: bg,
            borderRadius: BorderRadius.circular(14),
            border: Border.all(color: border),
          ),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              _LeadingIcon(isRead: item.isRead),
              horizontalSpace(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Expanded(
                          child: Text(
                            item.title,
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                            style: Styles.textTitle14Weight600.copyWith(
                              color: AppColors.whiteColor,
                            ),
                          ),
                        ),
                        horizontalSpace(width: 8),
                        Text(
                          item.timeAgo,
                          style: Styles.textTitle12Weight500.copyWith(
                            color: AppColors.unSelectedColor,
                          ),
                        ),
                      ],
                    ),
                    verticalSpace(height: 6),
                    Text(
                      item.message,
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                      style: Styles.textTitle12Weight500.copyWith(
                        color: AppColors.unSelectedColor,
                        height: 1.25,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _LeadingIcon extends StatelessWidget {
  final bool isRead;

  const _LeadingIcon({required this.isRead});

  @override
  Widget build(BuildContext context) {
    return Container(
      width: 42,
      height: 42,
      decoration: BoxDecoration(
        shape: BoxShape.circle,
        color: AppColors.formFilledColor,
        border: Border.all(color: AppColors.unSelectedColor.withOpacity(.35)),
      ),
      child: Center(
        child: SvgPicture.asset(
          'assets/images/icons_notification.svg',
          width: 24,
          height: 24,
        ),
      ),
    );
  }
}
