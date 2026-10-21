<?php

declare(strict_types=1);

use App\Http\Controllers\Api\V1\AuthController;
use App\Http\Controllers\Api\V1\Hub\TaskSnapshotController;
use App\Http\Controllers\Api\V1\Hub\TaskUpdateController;
use App\Http\Controllers\Api\V1\TaskController;
use Illuminate\Support\Facades\Route;

Route::prefix('v1')->group(function (): void {
    Route::post('auth/login', [AuthController::class, 'login'])->middleware('throttle:login');

    Route::middleware('auth:sanctum')->group(function (): void {
        Route::post('auth/logout', [AuthController::class, 'logout']);
        Route::get('auth/me', [AuthController::class, 'me']);

        Route::middleware('can:tasks.manage')->group(function (): void {
            Route::apiResource('tasks', TaskController::class);
        });

        Route::prefix('hub')->middleware(['can:hub.sync', 'throttle:hub'])->group(function (): void {
            Route::get('tasks', TaskSnapshotController::class);
            Route::post('task-updates', [TaskUpdateController::class, 'store']);
        });
    });
});
