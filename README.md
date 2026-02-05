# Markdown Viewer (Inspector)

This file is for validating the Inspector renderer.

- Bullet item
- Another item with `inline code`

> Quote block

---

## Code

```csharp
using UnityEngine;

public class Example : MonoBehaviour
{
    void Start() => Debug.Log("Hello");
}
```

## Table

| Name | Value |
| --- | --- |
| Foo | Bar |
| Number | 123 |

## Links

- External: https://unity.com/
- Asset link: `Assets/` (won't resolve, but should not break)

## Image

![Sample Image](Assets/SomeImage.png)
