module Shared.ReadModel

open System

[<CLIMutable>]
type VehicleOverview =
    { Id: int
      VehicleId: string
      Make: string
      Model: string
      Year: int
      Status: string
      AddedAt: DateTimeOffset
      UpdatedAt: DateTimeOffset }
