# Multi-stage contract projection impact map

**Authority-handoff mapping:** required

## Authority handoffs

| Stage/handoff | Input authority | Effective authority | Transformation | Downstream observable | Governing contract | Disagreement risk |
|---|---|---|---|---|---|---|
| Runtime descriptor creation | Declared read/write annotations and binder visibility | `CanRead`/`CanWrite`-gated runtime metadata | Discards declared write nullability when no effective write path exists | Runtime descriptor consumed by inline generation | Generated request contract describes runtime binder behavior | A hidden writer's declaration can disagree with binder visibility |
| Inline contract generation | Runtime descriptor | Effective runtime read/write metadata | Projects effective metadata into the inline representation | Inline generated contract | Runtime binder contract | Re-reading declarations would restore discarded state |
| Shared contract generation | Inline contract plus declared annotations | Effective inline nullability | Current path reconstructs write nullability from declarations | Shared generated contract consumed by serialization | Generated representations preserve effective binder behavior | Reconstruction can make the shared representation disagree with the inline contract |
| Contract serialization | Shared generated contract | Shared effective representation | Serializes generated nullability | Final contract document | Public generated contract | Consumers observe any authority drift introduced upstream |
