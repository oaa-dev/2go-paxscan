<?php

declare(strict_types=1);

use App\Enums\TaskStatusEnum;
use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        // Append-only. No FK on task_id: an update for an unknown task is still stored (rejected).
        Schema::create('task_status_updates', function (Blueprint $table): void {
            $table->uuid('id')->primary();
            $table->uuid('task_id');
            $table->enum('from_status', TaskStatusEnum::values());
            $table->enum('to_status', TaskStatusEnum::values());
            $table->unsignedInteger('base_version');
            $table->uuid('device_id');
            $table->unsignedBigInteger('device_seq');
            $table->timestampTz('device_created_at', 6);
            $table->uuid('hub_id');
            $table->timestampTz('hub_received_at', 6);
            $table->string('result', 20);
            $table->string('reject_reason', 100)->nullable();
            $table->timestampTz('created_at', 6)->useCurrent();

            $table->unique(['device_id', 'device_seq']);
            $table->index(['task_id', 'hub_received_at']);
        });
    }

    public function down(): void
    {
        Schema::dropIfExists('task_status_updates');
    }
};
