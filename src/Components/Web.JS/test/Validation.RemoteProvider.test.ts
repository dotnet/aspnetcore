import { expect, test, describe, beforeEach, afterEach, jest } from '@jest/globals';
import { remoteProvider } from '../src/Validation/RemoteProvider';
import { ValidatableElement } from '../src/Validation/Types';

// Mock fetch globally
const mockFetch = jest.fn<typeof fetch>();
(globalThis as any).fetch = mockFetch;

function mockJsonResponse(data: unknown, status = 200): Response {
  return {
    ok: status >= 200 && status < 300,
    status,
    json: () => Promise.resolve(data),
  } as Response;
}

function createFormWithFields(
  fields: { name: string; value: string }[]
): { form: HTMLFormElement; inputs: HTMLInputElement[] } {
  const form = document.createElement('form');
  const inputs: HTMLInputElement[] = [];
  for (const field of fields) {
    const input = document.createElement('input');
    input.setAttribute('name', field.name);
    input.value = field.value;
    form.appendChild(input);
    inputs.push(input);
  }
  return { form, inputs };
}

beforeEach(() => {
  mockFetch.mockReset();
});

describe('remote provider', () => {
  test('empty value returns true synchronously', () => {
    const el = document.createElement('input');
    el.setAttribute('name', 'Username');
    const result = remoteProvider('', el, { url: '/validate' });
    expect(result).toBe(true);
  });

  test('no url param returns true synchronously', () => {
    const el = document.createElement('input');
    el.setAttribute('name', 'Username');
    const result = remoteProvider('test', el, {});
    expect(result).toBe(true);
  });

  test('server returns true → valid', async () => {
    mockFetch.mockResolvedValue(mockJsonResponse(true));

    const { form, inputs } = createFormWithFields([
      { name: 'Username', value: 'john' },
    ]);

    const result = await remoteProvider('john', inputs[0], {
      url: '/api/validate',
      additionalfields: '*.Username',
    });

    expect(result).toBe(true);
    expect(mockFetch).toHaveBeenCalledTimes(1);

    // Should be GET by default
    const [calledUrl, options] = mockFetch.mock.calls[0];
    expect(calledUrl).toContain('/api/validate?');
    expect((options as RequestInit).method).toBe('GET');
  });

  test('server returns "true" string → valid', async () => {
    mockFetch.mockResolvedValue(mockJsonResponse('true'));

    const { inputs } = createFormWithFields([
      { name: 'Username', value: 'john' },
    ]);

    const result = await remoteProvider('john', inputs[0], { url: '/validate' });
    expect(result).toBe(true);
  });

  test('server returns false → invalid (use default message)', async () => {
    mockFetch.mockResolvedValue(mockJsonResponse(false));

    const { inputs } = createFormWithFields([
      { name: 'Username', value: 'taken' },
    ]);

    const result = await remoteProvider('taken', inputs[0], { url: '/validate' });
    expect(result).toBe(false);
  });

  test('server returns error string → custom error message', async () => {
    mockFetch.mockResolvedValue(mockJsonResponse('Username is already taken'));

    const { inputs } = createFormWithFields([
      { name: 'Username', value: 'taken' },
    ]);

    const result = await remoteProvider('taken', inputs[0], { url: '/validate' });
    expect(result).toBe('Username is already taken');
  });

  test('network error returns true (dont block user)', async () => {
    mockFetch.mockRejectedValue(new Error('Network error'));

    const { inputs } = createFormWithFields([
      { name: 'Username', value: 'test' },
    ]);

    const result = await remoteProvider('test', inputs[0], { url: '/validate' });
    expect(result).toBe(true);
  });

  test('cached result returns synchronously', async () => {
    mockFetch.mockResolvedValue(mockJsonResponse(true));

    const { inputs } = createFormWithFields([
      { name: 'Username', value: 'john' },
    ]);

    // First call — makes network request
    await remoteProvider('john', inputs[0], { url: '/validate' });
    expect(mockFetch).toHaveBeenCalledTimes(1);

    // Second call with same value — should return from cache (synchronous)
    const result = remoteProvider('john', inputs[0], { url: '/validate' });
    // Cached result is returned synchronously (not a Promise)
    expect(result).toBe(true);
    expect(mockFetch).toHaveBeenCalledTimes(1); // No additional fetch call
  });

  test('cache invalidated when value changes', async () => {
    mockFetch
      .mockResolvedValueOnce(mockJsonResponse(true))
      .mockResolvedValueOnce(mockJsonResponse(false));

    const { inputs } = createFormWithFields([
      { name: 'Username', value: 'john' },
    ]);

    // First call
    await remoteProvider('john', inputs[0], { url: '/validate' });
    expect(mockFetch).toHaveBeenCalledTimes(1);

    // Different value — should make new request
    inputs[0].value = 'jane';
    const result = await remoteProvider('jane', inputs[0], { url: '/validate' });
    expect(mockFetch).toHaveBeenCalledTimes(2);
    expect(result).toBe(false);
  });

  test('additional fields are collected from form', async () => {
    mockFetch.mockResolvedValue(mockJsonResponse(true));

    const { inputs } = createFormWithFields([
      { name: 'Model.Username', value: 'john' },
      { name: 'Model.Email', value: 'john@example.com' },
    ]);

    await remoteProvider('john', inputs[0], {
      url: '/validate',
      additionalfields: '*.Username,*.Email',
    });

    const [calledUrl] = mockFetch.mock.calls[0];
    const url = new URL(calledUrl as string, 'http://localhost');
    expect(url.searchParams.get('Model.Username')).toBe('john');
    expect(url.searchParams.get('Model.Email')).toBe('john@example.com');
  });

  test('POST method sends body', async () => {
    mockFetch.mockResolvedValue(mockJsonResponse(true));

    const { inputs } = createFormWithFields([
      { name: 'Username', value: 'john' },
    ]);

    await remoteProvider('john', inputs[0], {
      url: '/validate',
      type: 'Post',
    });

    const [calledUrl, options] = mockFetch.mock.calls[0];
    expect(calledUrl).toBe('/validate');
    expect((options as RequestInit).method).toBe('POST');
    expect((options as RequestInit).body).toContain('Username=john');
    expect((options as RequestInit).headers).toEqual({
      'Content-Type': 'application/x-www-form-urlencoded',
    });
  });

  test('*.PropertyName resolution for nested models', async () => {
    mockFetch.mockResolvedValue(mockJsonResponse(true));

    const { inputs } = createFormWithFields([
      { name: 'User.Profile.Name', value: 'john' },
      { name: 'User.Profile.Country', value: 'US' },
    ]);

    await remoteProvider('john', inputs[0], {
      url: '/validate',
      additionalfields: '*.Name,*.Country',
    });

    const [calledUrl] = mockFetch.mock.calls[0];
    const url = new URL(calledUrl as string, 'http://localhost');
    expect(url.searchParams.get('User.Profile.Name')).toBe('john');
    expect(url.searchParams.get('User.Profile.Country')).toBe('US');
  });
});
