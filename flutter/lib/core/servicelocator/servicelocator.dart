import 'package:get_it/get_it.dart';

import '../../features/auth/data/repositories/auth_repository.dart';
import '../../features/auth/newtwork/auth_interceptor.dart';
import '../../features/auth/presentation/bloc/auth_bloc.dart';
import '../../features/auth/services/token_storage_service.dart';

import '../../features/calender/data/repo/calendar_repo.dart';
import '../../features/calender/presentation/cubit/calendar_cubit.dart';
import '../../features/notification/data/data_sources/notifications_service.dart';
import '../../features/notification/data/repositories/notifications_repo.dart';
import '../../features/notification/data/repositories/push_notification_service.dart';
import '../../features/notification/presentation/cubit/notifications_cubit.dart';

import '../../features/leave_request/data/data_sources/leave_data_source.dart';
import '../../features/leave_request/data/repositories/leave_repository.dart';
import '../../features/leaves/data/data_sources/leaves_data_source.dart';
import '../../features/leaves/data/repositories/leaves_repository.dart';
import '../../features/home/data/data_sources/home_data_source.dart';
import '../../features/home/data/repositories/home_repository.dart';
import '../../features/home/presentation/cubit/home_cubit.dart';

import '../../features/user/data/repositories/user_repository.dart';
import '../../features/user/presentation/cubit/user_cubit.dart';

import '../locale/locale_cubit.dart';
import '../network/dio_client.dart';
import '../theme/theme_cubit.dart';

final GetIt sl = GetIt.instance;

Future<void> initDependencies() async {
  // ─────────────────────────────────────────────────────────────
  // Core Services
  // ─────────────────────────────────────────────────────────────
  sl.registerLazySingleton<TokenStorageService>(() => TokenStorageService());

  // ─────────────────────────────────────────────────────────────
  // Networking (avoid circular dependency)
  // ─────────────────────────────────────────────────────────────
  sl.registerLazySingleton<DioClient>(() {
    final dioClient = DioClient();

    final authInterceptor = AuthInterceptor(
      sl<TokenStorageService>(),
      dioClient,
    );

    dioClient.dio.interceptors.insert(0, authInterceptor);
    return dioClient;
  });

  // ─────────────────────────────────────────────────────────────
  // Repositories / DataSources
  // ─────────────────────────────────────────────────────────────
  sl.registerLazySingleton<AuthRepository>(
        () => AuthRepository(sl<DioClient>(), sl<TokenStorageService>()),
  );

  sl.registerLazySingleton<UserRepository>(
        () => UserRepository(sl<DioClient>()),
  );

  // Notifications
  sl.registerLazySingleton<NotificationsService>(
        () => NotificationsService(sl<DioClient>()),
  );

  sl.registerLazySingleton<NotificationsRepo>(
        () => NotificationsRepo(sl<NotificationsService>()),
  );

  // Leaves

  sl.registerLazySingleton<LeaveDataSource>(
        () => LeaveDataSource(sl<DioClient>()),
  );

  sl.registerLazySingleton<LeaveRepository>(
        () => LeaveRepository(sl<LeaveDataSource>()),
  );

  sl.registerLazySingleton<LeavesDataSource>(
        () => LeavesDataSource(sl<DioClient>()),
  );

  sl.registerLazySingleton<LeavesRepository>(
        () => LeavesRepository(sl<LeavesDataSource>()),
  );

  // Home
  sl.registerLazySingleton<HomeDataSource>(
        () => HomeDataSource(sl<DioClient>()),
  );

  sl.registerLazySingleton<HomeRepository>(
        () => HomeRepository(sl<HomeDataSource>()),
  );

  // ─────────────────────────────────────────────────────────────
  // BLoCs / Cubits
  // ─────────────────────────────────────────────────────────────
  sl.registerLazySingleton<AuthBloc>(
        () => AuthBloc(
      authRepository: sl<AuthRepository>(),
      tokenStorage: sl<TokenStorageService>(),
          push: sl()
    ),
  );

  sl.registerLazySingleton<PushNotificationService>(
        () => PushNotificationService.instance,
  );


  sl.registerLazySingleton<UserCubit>(
        () => UserCubit(sl<UserRepository>()),
  );

  sl.registerFactory<NotificationsCubit>(
        () => NotificationsCubit(sl<NotificationsRepo>()),
  );

  sl.registerLazySingleton<ThemeCubit>(() => ThemeCubit());
  sl.registerLazySingleton<LocaleCubit>(() => LocaleCubit());
  sl.registerLazySingleton<CalendarRepository>(() => CalendarRepository(sl<DioClient>()));
  sl.registerFactory<CalendarCubit>(() => CalendarCubit(sl<CalendarRepository>()));

  // Home Cubit - singleton so we can refresh notification count globally
  sl.registerLazySingleton<HomeCubit>(
        () => HomeCubit(sl<HomeRepository>()),
  );
}