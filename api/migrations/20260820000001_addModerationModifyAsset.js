/**
 * db tables
 * @param {import('knex')} knex
 */
exports.up = async (knex) => {
    await knex.schema.createTable('moderation_modify_asset', (t) => {
        t.bigIncrements('id').notNullable().unsigned();
        t.bigInteger('asset_id').notNullable().unsigned();
        t.bigInteger('actor_id').notNullable().unsigned();
        t.string('old_name', 100).nullable();
        t.string('new_name', 100).nullable();
        t.string('old_description', 1000).nullable();
        t.string('new_description', 1000).nullable();
        t.dateTime('created_at').notNullable().defaultTo(knex.fn.now());

        t.index(['asset_id']);
        t.index(['actor_id']);
    });
};

exports.down = async (knex) => {
    await knex.schema.dropTable('moderation_modify_asset');
};