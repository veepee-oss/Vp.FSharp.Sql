# An attempt at keeping a trace of design decisions

# 👩‍🎨Premise, kinda

> Haters will hate, lovers will love.
> 
> We love y'all, even if you deeply hate and despise us.

![We love y'all](https://media.giphy.com/media/Pio2NkVIbTWMaYiKeV/giphy.gif)

# ❔Why did you folks create this lib?

## 🙋Motivations

- We all agree that Dapper is a great library.
- We also wanted something even more explicit and more functional / F#-idiomatic
- Inspired by [Zaid's Npgsql.FSharp](https://github.com/Zaid-Ajaj/Npgsql.FSharp) but with
    - Multi DB-providers support through specialization (but no specialization through inheritance).
    - It might sound controversial (again) to some people in the F# community to not rely heavily on OO for 
      this kind of design decision but "haters will hate" and we, well, we will be just fine, we love you, regardless.
    - Explicit operations flow
    - `Transaction` and `TransactionScope` Helpers
    - Dapper connection workflow
    - (Very) limited support for basic events (ie. logging)
    - Opinionated:
      - No connection wrapping: explicit
      - `Async<'T>`-only
      - No result as return types: our premise is that we do expect infra (IO) to run as smoothly as possible, 
        if you think that something might fail, it's your duty to implement the relevant resilience policy.
- There is definitely a lot of room for improvements, it's definitely not a ground-breaking library but some people might find it handy.

## 🙎‍♀️ F# Limitations, Shortcomings, Discrepancies?

### 🚣‍♀️SRTP

[GitHub issue: Return types in shadowing members are not considered in generic constraint resolution to avoid ambiguity](https://github.com/dotnet/fsharp/issues/8794)

Can't use SRTP with shadowing members which sadly, is a very common practice in the implementation of most ADO.NET providers.

### 🧗‍♀️Type Inference

[GitHub issue: Avoid type inference defaulting to `object` in absence of evidence](https://github.com/fsharp/fslang-suggestions/issues/885)

We could have a much simpler implementation to avoid specifying a certain number of generic constraints it wasn't automatically inferred to `obj` by the F# compiler.

## 🤸‍♀️Decisions

The core library ships the main building blocks so that you can create your own SQL libraries.

Those building blocks must therefore respect the conditions below:

- Relatively small modules 
- Limited Scope
- Versatile (generic constraints are kinda loose)
- Opinionated 
- **Retain some of the underlying ADO.NET provider specific types**
