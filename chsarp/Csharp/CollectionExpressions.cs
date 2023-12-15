﻿namespace Csharp;

public class CollectionExpressions
{
    public void EmptyCollectionInitialization()
    {
        int[] intArray = [];
        List<int> intList = [];
        Dictionary<string, int> dictionary = [];
    }

    public void CollectionInitialization()
    {
        int[] intArray = [1, 2, 3, 4, 5];
        List<int> intList = [1, 2, 3, 4, 5];
        Dictionary<string, string> dictionary = new() { ["key"] = "value" };

        int[] intCollection = [.. intArray, .. intList];
        int[] anotherIntCollection = [.. intCollection, 1, 2, 3, 4, 5];
    }
}
