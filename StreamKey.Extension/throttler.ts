export class ThrottledFetcher<T> {
  private lastCall = 0;
  private lastResult: T | null = null;

  constructor(
    private readonly fetchFn: () => Promise<T>,
    private readonly intervalMs: number = 30_000
  ) {}

  async fetch(): Promise<T | null> {
    const now = Date.now();

    if (now - this.lastCall < this.intervalMs && this.lastResult !== null) {
      return this.lastResult;
    }

    this.lastCall = now;
    this.lastResult = await this.fetchFn();
    return this.lastResult;
  }
}

export class ThrottledRunner {
  private lastCall = 0;

  constructor(
    private readonly runFn: () => Promise<void>,
    private readonly intervalMs: number = 30_000
  ) {}

  async run(): Promise<void> {
    const now = Date.now();

    if (now - this.lastCall < this.intervalMs) {
      return;
    }

    this.lastCall = now;
    await this.runFn();
  }
}
