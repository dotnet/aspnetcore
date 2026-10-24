const start = '<!-- aspnetcore-pr-docs-check-author -->';
const end = '<!-- /aspnetcore-pr-docs-check-author -->';

function authorBody(body, author, sourceRepository, sourcePrNumber) {
  if (typeof body !== 'string') {
    throw new Error('The validated docs PR must have a body.');
  }
  if (sourceRepository !== 'dotnet/aspnetcore' || !Number.isSafeInteger(sourcePrNumber) || sourcePrNumber <= 0) {
    throw new Error('Invalid source identity for docs PR attribution.');
  }
  const blocks = [];
  let offset = 0;
  while (body.indexOf(start, offset) !== -1) {
    const first = body.indexOf(start, offset);
    const priorEnd = body.indexOf(end, offset);
    const last = body.indexOf(end, first + start.length);
    const next = body.indexOf(start, first + start.length);
    if (last === -1 || (next !== -1 && next < last) || (priorEnd !== -1 && priorEnd < first)) {
      throw new Error('Malformed managed author section; the docs PR body was not changed.');
    }
    blocks.push([first, last + end.length]);
    offset = last + end.length;
  }
  if (body.indexOf(end, offset) !== -1) {
    throw new Error('Unmatched managed author section; the docs PR body was not changed.');
  }
  const human = author?.type === 'User' && !author.login?.endsWith('[bot]');
  if (human && !/^[A-Za-z0-9](?:[A-Za-z0-9-]{0,37}[A-Za-z0-9])?$/.test(author.login || '')) {
    throw new Error('Invalid GitHub author login.');
  }
  const attribution = human ? [
    start,
    `@${author.login}, this draft documents your source change in ${sourceRepository}#${sourcePrNumber}.`,
    '',
    'Please inspect this draft for technical accuracy and confirm that the user impact and recommended guidance match the implementation.',
    end,
  ].join('\n') : '';
  if (!blocks.length) {
    return attribution ? `${body}\n\n${attribution}` : body;
  }
  let result = '';
  offset = 0;
  for (const [index, [first, last]] of blocks.entries()) {
    result += body.slice(offset, first) + (index === 0 ? attribution : '');
    offset = last;
  }
  return result + body.slice(offset);
}

module.exports = { authorBody };
