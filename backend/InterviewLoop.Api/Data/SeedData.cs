using InterviewLoop.Api.Models;

namespace InterviewLoop.Api.Data;

public static class SeedData
{
    public static void EnsureSeeded(InterviewLoopDbContext db)
    {
        if (db.Prompts.Any()) return;

        db.Prompts.AddRange(
            new Prompt
            {
                Title = "Two Sum",
                Difficulty = "Easy",
                Description = "Given an array of integers `nums` and an integer `target`, return the indices of the two numbers that add up to `target`. Assume exactly one solution exists and you may not use the same element twice. Aim for better than O(n^2) time.",
                StarterCode = "function twoSum(nums, target) {\n  // your code here\n}"
            },
            new Prompt
            {
                Title = "Longest Substring Without Repeating Characters",
                Difficulty = "Medium",
                Description = "Given a string `s`, find the length of the longest substring without repeating characters. Aim for O(n) time using a sliding window.",
                StarterCode = "function lengthOfLongestSubstring(s) {\n  // your code here\n}"
            },
            new Prompt
            {
                Title = "Valid Parentheses",
                Difficulty = "Easy",
                Description = "Given a string containing just the characters '(', ')', '{', '}', '[' and ']', determine if the input string is valid (every open bracket is closed by the same type of bracket, in the correct order).",
                StarterCode = "function isValid(s) {\n  // your code here\n}"
            },
            new Prompt
            {
                Title = "Merge Intervals",
                Difficulty = "Medium",
                Description = "Given an array of intervals where intervals[i] = [start_i, end_i], merge all overlapping intervals and return an array of the non-overlapping intervals that cover all the intervals in the input.",
                StarterCode = "function merge(intervals) {\n  // your code here\n}"
            },
            new Prompt
            {
                Title = "Group Anagrams",
                Difficulty = "Medium",
                Description = "Given an array of strings, group the anagrams together. You can return the answer in any order.",
                StarterCode = "function groupAnagrams(strs) {\n  // your code here\n}"
            },
            new Prompt
            {
                Title = "Binary Tree Level Order Traversal",
                Difficulty = "Medium",
                Description = "Given the root of a binary tree, return the level order traversal of its nodes' values (i.e., from left to right, level by level), as an array of arrays.",
                StarterCode = "function levelOrder(root) {\n  // your code here\n}"
            },
            new Prompt
            {
                Title = "Coin Change",
                Difficulty = "Hard",
                Description = "You are given an integer array `coins` representing coin denominations and an integer `amount`. Return the fewest number of coins needed to make up `amount`. If that amount cannot be made up, return -1.",
                StarterCode = "function coinChange(coins, amount) {\n  // your code here\n}"
            },
            new Prompt
            {
                Title = "Number of Islands",
                Difficulty = "Medium",
                Description = "Given an m x n 2D binary grid which represents a map of '1's (land) and '0's (water), return the number of islands. An island is surrounded by water and formed by connecting adjacent lands horizontally or vertically.",
                StarterCode = "function numIslands(grid) {\n  // your code here\n}"
            },
            new Prompt
            {
                Title = "LRU Cache",
                Difficulty = "Hard",
                Description = "Design a data structure that follows the constraints of a Least Recently Used (LRU) cache. Implement `get(key)` and `put(key, value)` so both run in O(1) average time.",
                StarterCode = "class LRUCache {\n  constructor(capacity) {\n    // your code here\n  }\n  get(key) {\n\n  }\n  put(key, value) {\n\n  }\n}"
            },
            new Prompt
            {
                Title = "Kth Largest Element in an Array",
                Difficulty = "Medium",
                Description = "Given an integer array `nums` and an integer `k`, return the kth largest element in the array. Note it is the kth largest in sorted order, not the kth distinct element. Aim for better than O(n log n) if possible.",
                StarterCode = "function findKthLargest(nums, k) {\n  // your code here\n}"
            }
        );

        db.SaveChanges();
    }
}
