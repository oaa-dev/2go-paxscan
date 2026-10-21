<?php

declare(strict_types=1);

namespace Database\Factories;

use App\Enums\TaskStatusEnum;
use App\Enums\TaskTypeEnum;
use App\Models\Task;
use Illuminate\Database\Eloquent\Factories\Factory;

/** @extends Factory<Task> */
class TaskFactory extends Factory
{
    public function definition(): array
    {
        return [
            'name' => fake()->sentence(3),
            'type' => fake()->randomElement(TaskTypeEnum::cases()),
            'description' => fake()->optional()->sentence(),
            'status' => TaskStatusEnum::Pending,
            'version' => 1,
        ];
    }

    public function chef(): static
    {
        return $this->state(['type' => TaskTypeEnum::Chef]);
    }

    public function guard(): static
    {
        return $this->state(['type' => TaskTypeEnum::Guard]);
    }
}
