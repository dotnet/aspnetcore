// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { readFileSync } from 'node:fs';
import { pathToFileURL } from 'node:url';

const reviewCases = {
  'case-01': { section: 'DISCARDED', confidence: 'discarded' },
  'case-02': { section: 'NEW_FINDINGS', confidence: 'verified' },
  'case-03': { section: 'DISCARDED', confidence: 'discarded' },
  'case-04': { section: 'UNRESOLVED', confidence: 'unresolved' },
  'case-05': { section: 'EXISTING_FEEDBACK_COVERAGE', thread: 'discussion_r4487936812' },
  'case-06': { section: 'UNRESOLVED', confidence: 'unresolved' },
};

const componentCases = {
  a: { decision: 'DROP', dropBasis: 'unestablished' },
  b: { decision: 'KEEP' },
  c: { decision: 'KEEP' },
};

function normalizedResult(output)
{
  const text = output.trim().replace(/^```(?:json)?\s*\n([\s\S]*?)\n```\s*$/i, '$1');
  const parsed = JSON.parse(text);
  if (parsed && Array.isArray(parsed.results))
  {
    return parsed.results.length === 1 ? parsed.results[0] : null;
  }
  return parsed;
}

function gradeComponent(name, output)
{
  const expected = componentCases[name];
  if (!expected)
  {
    throw new Error(`Unknown component case: ${name}`);
  }
  let actual;
  try
  {
    actual = normalizedResult(output);
  }
  catch (error)
  {
    if (!(error instanceof SyntaxError))
    {
      throw error;
    }
    return false;
  }
  if (!actual || typeof actual !== 'object' || Array.isArray(actual))
  {
    return false;
  }
  const normalized = key => typeof actual[key] === 'string' ? actual[key].trim().toLowerCase() : '';
  if (!Object.entries(expected).every(([key, value]) => normalized(key) === value.toLowerCase()))
  {
    return false;
  }
  // Inapplicable fields may be omitted, null, or explicitly marked none.
  return ['downgradeKind', 'dropBasis', 'contradictedLink']
    .filter(key => !(key in expected))
    .every(key => actual[key] == null || ['', 'none', 'n/a'].includes(normalized(key)));
}

function gradeReview(name, output)
{
  const expected = reviewCases[name];
  if (!expected)
  {
    throw new Error(`Unknown review case: ${name}`);
  }
  const lines = output.trim().split(/\r?\n/);
  if (!/^STATUS: (FINDINGS|NO_FINDINGS)$/.test(lines[0]))
  {
    return false;
  }
  const sections = new Map();
  let section;
  for (const line of lines.slice(1))
  {
    const heading = line.trim().replace(/^#{1,6}\s+/, '').replace(/^\*\*|\*\*$/g, '').replace(/:$/, '');
    if (['NEW_FINDINGS', 'EXISTING_FEEDBACK_COVERAGE', 'UNRESOLVED', 'DISCARDED'].includes(heading))
    {
      if (sections.has(heading))
      {
        return false;
      }
      section = heading;
      sections.set(section, '');
    }
    else if (section)
    {
      sections.set(section, sections.get(section) + '\n' + line);
    }
  }
  if (sections.size !== 4)
  {
    return false;
  }
  if ([...sections.values()].some(text => /\bC(?!1\b)\d+\b/.test(text)))
  {
    return false;
  }
  const hasCandidate = key => /\bC1\b/.test(sections.get(key));
  if (!hasCandidate(expected.section))
  {
    return false;
  }
  if (expected.thread)
  {
    return !hasCandidate('NEW_FINDINGS') &&
      !hasCandidate('DISCARDED') &&
      hasCandidate('UNRESOLVED') &&
      sections.get(expected.section).includes(expected.thread) &&
      sections.get('UNRESOLVED').includes(expected.thread) &&
      lines[0] === 'STATUS: NO_FINDINGS';
  }
  if (expected.section !== 'UNRESOLVED' && hasCandidate('UNRESOLVED') ||
      expected.section !== 'DISCARDED' && hasCandidate('DISCARDED') ||
      hasCandidate('EXISTING_FEEDBACK_COVERAGE'))
  {
    return false;
  }
  const inline = sections.get('NEW_FINDINGS');
  const hasInline = hasCandidate('NEW_FINDINGS');
  if (!hasInline)
  {
    return false;
  }
  if (hasInline)
  {
    const confidences = [...inline.matchAll(/\bconfidence\s*:\s*([a-z]+)/gi)].map(match => match[1].toLowerCase());
    if (confidences.length !== 1 || confidences[0] !== expected.confidence ||
        lines[0] !== 'STATUS: FINDINGS')
    {
      return false;
    }
    if (expected.confidence !== 'verified' &&
        (!inline.includes(`[${expected.confidence.toUpperCase()}]`) ||
         !new RegExp(`Why ${expected.confidence}:`, 'i').test(inline)))
    {
      return false;
    }
  }
  return hasInline || lines[0] === 'STATUS: NO_FINDINGS';
}

export function grade(input)
{
  const output = input.trajectory?.output;
  const name = input.stimulus?.name;
  const mode = input.config?.env?.DISPOSITION_MODE;
  if (typeof output !== 'string' || typeof name !== 'string')
  {
    throw new Error('The grader requires final trajectory.output and stimulus.name.');
  }
  const passed = mode === 'review' ? gradeReview(name, output) :
    mode === 'component' ? gradeComponent(name, output) :
    (() => { throw new Error(`Unknown disposition mode: ${mode}`); })();
  return {
    name: 'disposition',
    kind: 'code',
    passed,
    score: passed ? 1 : 0,
    evidence: passed ? 'Expected normalized disposition and placement.' : 'Wrong or missing normalized disposition or placement.',
  };
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href)
{
  console.log(JSON.stringify(grade(JSON.parse(readFileSync(process.env.EVALUATE_GRADER_INPUT, 'utf8')))));
}
